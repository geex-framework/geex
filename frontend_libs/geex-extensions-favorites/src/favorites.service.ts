import { DestroyRef, inject, Injectable, Injector, signal } from "@angular/core";
import type { ResolveFn } from "@angular/router";
import { Apollo } from "apollo-angular";
import { OAuthService } from "angular-oauth2-oidc";
import { firstValueFrom } from "rxjs";
import { ADD_FAVORITE, FAVORITES, FAVORITE_STATUSES, FAVORITE_TYPES, REMOVE_FAVORITE } from "./graphql";
import type { FavoriteListVariables, FavoritePage, FavoriteState, FavoriteStatus, FavoriteTypeConfiguration } from "./favorites.types";

@Injectable()
export class FavoritesService {
  readonly types = signal<FavoriteTypeConfiguration[]>([]);
  readonly states = signal<Record<string, FavoriteState>>({});
  readonly changes = signal(0);
  private readonly apollo: Apollo;
  private readonly oauth: OAuthService;
  private generation = 0;
  private configurationRequest = 0;
  private initialized?: Promise<void>;
  private readonly queued = new Map<string, Set<string>>();
  private scheduled = false;
  private versions = new Map<string, number>();

  constructor(injector: Injector) {
    this.apollo = injector.get(Apollo);
    this.oauth = injector.get(OAuthService);
    const events = this.oauth.events.subscribe(event => {
      if (["logout", "session_terminated", "session_error"].includes(event.type)) this.reset();
      if (event.type === "token_received") void this.init(true);
    });
    injector.get(DestroyRef).onDestroy(() => { this.reset(); events.unsubscribe(); });
  }

  init(force = false): Promise<void> {
    if (force) this.reset();
    return this.initialized ??= this.oauth.hasValidAccessToken() ? this.refresh() : Promise.resolve();
  }

  reset(): void {
    this.generation++;
    this.configurationRequest++;
    this.initialized = undefined;
    this.queued.clear();
    this.versions.clear();
    this.types.set([]);
    this.states.set({});
    this.changes.update(value => value + 1);
  }

  async refresh(): Promise<void> {
    const request = ++this.configurationRequest;
    const response = await firstValueFrom(this.apollo.query<{ favoriteEntityTypes: FavoriteTypeConfiguration[] }>({
      query: FAVORITE_TYPES, fetchPolicy: "no-cache", errorPolicy: "none",
    }));
    if (!response.data?.favoriteEntityTypes) throw new Error("Missing favoriteEntityTypes response");
    if (request !== this.configurationRequest) return;
    this.generation++;
    this.queued.clear();
    this.states.set({});
    this.types.set(response.data.favoriteEntityTypes);
    this.changes.update(value => value + 1);
  }

  enabled(type: string): boolean { return this.types().some(item => item.entityType === type); }
  key(type: string, id: string): string { return JSON.stringify([type, id]); }
  state(type: string, id: string): FavoriteState {
    return this.states()[this.key(type, id)] ?? { entityType: type, entityId: id, isFavorite: false, loaded: false, pending: false };
  }
  private setState(type: string, id: string, state: Partial<FavoriteState>): void {
    this.states.update(current => ({ ...current, [this.key(type, id)]: { ...this.state(type, id), ...state } }));
  }

  requestStatus(type: string, id: string, retry = false): void {
    if (!id || !this.enabled(type)) return;
    const current = this.state(type, id);
    if (current.pending || (current.loaded && !retry)) return;
    this.setState(type, id, { pending: true, error: undefined });
    if (!this.queued.has(type)) this.queued.set(type, new Set());
    this.queued.get(type)!.add(id);
    if (!this.scheduled) {
      this.scheduled = true;
      queueMicrotask(() => { this.scheduled = false; void this.flushStatuses(); });
    }
  }

  private async flushStatuses(): Promise<void> {
    const generation = this.generation;
    const batches = [...this.queued];
    this.queued.clear();
    await Promise.all(batches.map(async ([entityType, ids]) => {
      const allIds = [...ids];
      for (let offset = 0; offset < allIds.length; offset += 500) {
        const entityIds = allIds.slice(offset, offset + 500);
        const versions = entityIds.map(id => this.versions.get(this.key(entityType, id)) ?? 0);
        try {
          const response = await firstValueFrom(this.apollo.query<{ favoriteStatuses: FavoriteStatus[] }>({
            query: FAVORITE_STATUSES, variables: { entityType, entityIds }, fetchPolicy: "no-cache", errorPolicy: "none",
          }));
          const statuses = response.data?.favoriteStatuses;
          if (!statuses || entityIds.some(id => !statuses.some(item => item.entityId === id)))
            throw new Error("Incomplete favoriteStatuses response");
          if (generation !== this.generation) return;
          entityIds.forEach((id, index) => {
            if ((this.versions.get(this.key(entityType, id)) ?? 0) === versions[index])
              this.setState(entityType, id, { ...statuses.find(item => item.entityId === id)!, loaded: true, pending: false });
          });
        } catch (error) {
          if (generation !== this.generation) return;
          entityIds.forEach((id, index) => {
            if ((this.versions.get(this.key(entityType, id)) ?? 0) === versions[index])
              this.setState(entityType, id, { loaded: true, pending: false, error });
          });
        }
      }
    }));
  }

  async setFavorite(entityType: string, entityId: string, isFavorite: boolean): Promise<void> {
    if (!this.enabled(entityType) || this.state(entityType, entityId).pending) return;
    const generation = this.generation;
    const key = this.key(entityType, entityId);
    this.versions.set(key, (this.versions.get(key) ?? 0) + 1);
    this.setState(entityType, entityId, { pending: true, error: undefined });
    try {
      const response = await firstValueFrom(this.apollo.mutate<{ addFavorite?: FavoriteStatus; removeFavorite?: boolean }>({
        mutation: isFavorite ? ADD_FAVORITE : REMOVE_FAVORITE,
        variables: { request: { entityType, entityId } }, errorPolicy: "none",
      }));
      if (isFavorite ? !response.data?.addFavorite?.isFavorite : response.data?.removeFavorite !== true)
        throw new Error("Favorite mutation was not confirmed");
      if (generation !== this.generation) return;
      this.setState(entityType, entityId, {
        ...(isFavorite ? response.data!.addFavorite : { isFavorite: false, createdOn: null }), loaded: true, pending: false,
      });
      this.changes.update(value => value + 1);
    } catch (error) {
      if (generation === this.generation) this.setState(entityType, entityId, { pending: false });
      throw error;
    }
  }

  async list(variables: FavoriteListVariables): Promise<FavoritePage> {
    const generation = this.generation;
    const revision = this.changes();
    const response = await firstValueFrom(this.apollo.query<{ favorites: FavoritePage }>({
      query: FAVORITES, variables, fetchPolicy: "no-cache", errorPolicy: "none",
    }));
    if (!response.data?.favorites?.items || typeof response.data.favorites.totalCount !== "number")
      throw new Error("Incomplete favorites response");
    if (generation !== this.generation) return { items: [], totalCount: 0 };
    for (const item of response.data.favorites.items) {
      if (revision === this.changes() && !this.state(item.entityType, item.entityId).pending)
        this.setState(item.entityType, item.entityId, { isFavorite: true, createdOn: item.createdOn, loaded: true });
    }
    return response.data.favorites;
  }
}

export const resolveFavoriteTypes: ResolveFn<void> = () => inject(FavoritesService, { optional: true })?.refresh();
