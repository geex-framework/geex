import { inject, LOCALE_ID } from "@angular/core";
import { GEEX_I18N } from "@geexcode/geex-angular";

export interface FavoriteLabels {
  title: string;
  name: string;
  createdOn: string;
  actions: string;
  add: string;
  saved?: string;
  remove: string;
  retry: string;
  failed: string;
  empty: string;
  entityTypes: Record<string, string>;
}
export const favoritesZhCN: FavoriteLabels = {
  title: "我的收藏", name: "名称", createdOn: "收藏时间", actions: "操作",
  add: "收藏", saved: "已收藏", remove: "取消收藏", retry: "重试", failed: "收藏操作失败, 请重试.", empty: "暂无收藏",
  entityTypes: { ApprovalFlow: "审批流" },
};
export const favoritesEnUS: FavoriteLabels = {
  title: "My favorites", name: "Name", createdOn: "Favorited at", actions: "Actions",
  add: "Favorite", saved: "Favorited", remove: "Unfavorite", retry: "Retry", failed: "Could not update favorites. Please retry.", empty: "No favorites",
  entityTypes: { ApprovalFlow: "Approval flows" },
};
export function injectFavoriteLabels(): () => FavoriteLabels {
  const dictionary = inject(GEEX_I18N, { optional: true }) as { Favorites?: Partial<FavoriteLabels> } | null;
  const fallback = inject(LOCALE_ID).startsWith("zh") ? favoritesZhCN : favoritesEnUS;
  return () => ({ ...fallback, ...dictionary?.Favorites, entityTypes: { ...fallback.entityTypes, ...dictionary?.Favorites?.entityTypes } });
}
