import { Routes } from "@angular/router";

export const blobStorageRoutes: Routes = [
  { path: "", loadChildren: () => import("./pages/blob-storage-pages.routes").then(m => m.blobStoragePagesRoutes) },
];
