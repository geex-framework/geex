import { gql } from "apollo-angular";

export const FAVORITE_TYPES = gql`query favoriteEntityTypes {
  favoriteEntityTypes { entityType nameField detailRoute }
}`;
export const FAVORITE_STATUSES = gql`query favoriteStatuses($entityType: FavoriteEntityType!, $entityIds: [String!]!) {
  favoriteStatuses(entityType: $entityType, entityIds: $entityIds) { entityType entityId isFavorite createdOn }
}`;
export const FAVORITES = gql`query favorites($entityType: FavoriteEntityType!, $skip: Int!, $take: Int!, $filter: FavoriteItemFilterInput, $sort: [FavoriteItemSortInput!]) {
  favorites(entityType: $entityType, skip: $skip, take: $take, filter: $filter, sort: $sort) {
    items { id entityType entityId createdOn displayValue detailUrl }
    totalCount
  }
}`;
export const ADD_FAVORITE = gql`mutation addFavorite($request: FavoriteReferenceInput!) {
  addFavorite(request: $request) { entityType entityId isFavorite createdOn }
}`;
export const REMOVE_FAVORITE = gql`mutation removeFavorite($request: FavoriteReferenceInput!) {
  removeFavorite(request: $request)
}`;
