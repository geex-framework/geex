import type { FavoriteListVariables } from "./favorites.types";

export interface FavoriteQueryState {
  pageIndex: number;
  pageSize: number;
  sort: "ascend" | "descend";
  dates: Date[];
}
export function createFavoriteQueryState(): FavoriteQueryState {
  return { pageIndex: 1, pageSize: 10, sort: "descend", dates: [] };
}
export function favoriteListVariables(entityType: string, state: FavoriteQueryState): FavoriteListVariables {
  let filter: FavoriteListVariables["filter"];
  if (state.dates.length === 2 && state.dates.every(date => !Number.isNaN(date.getTime()))) {
    const from = new Date(state.dates[0]);
    const until = new Date(state.dates[1]);
    from.setHours(0, 0, 0, 0);
    until.setHours(0, 0, 0, 0);
    until.setDate(until.getDate() + 1);
    filter = { createdOn: { gte: from.toISOString(), lt: until.toISOString() } };
  }
  const direction = state.sort;
  return {
    entityType, skip: (state.pageIndex - 1) * state.pageSize, take: state.pageSize,
    filter, sort: [{ createdOn: direction }, { id: direction }],
  };
}
