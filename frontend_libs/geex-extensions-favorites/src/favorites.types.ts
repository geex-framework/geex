import type { Signal } from "@angular/core";
import type { GeexModule } from "@geexcode/geex-angular";

export interface FavoriteTypeConfiguration {
  entityType: string;
  nameField: string;
  detailRoute?: string | null;
}
export interface FavoriteStatus {
  entityType: string;
  entityId: string;
  isFavorite: boolean;
  createdOn?: string | null;
}
export interface FavoriteState extends FavoriteStatus {
  loaded: boolean;
  pending: boolean;
  error?: unknown;
}
export interface FavoriteItem extends Omit<FavoriteStatus, "isFavorite"> {
  id: string;
  displayValue: string;
  detailUrl?: string | null;
}
export interface FavoritePage { items: FavoriteItem[]; totalCount: number; }
export interface FavoriteListVariables {
  entityType: string;
  skip: number;
  take: number;
  filter?: { createdOn: { gte?: string; lt?: string } };
  sort?: Array<{ createdOn?: "ascend" | "descend"; id?: "ascend" | "descend" }>;
}
export interface FavoritesModule extends GeexModule {
  types: Signal<FavoriteTypeConfiguration[]>;
  refresh(): Promise<void>;
}
declare module "@geexcode/geex-angular" {
  interface GeexModuleMap { favorites: FavoritesModule; }
}
