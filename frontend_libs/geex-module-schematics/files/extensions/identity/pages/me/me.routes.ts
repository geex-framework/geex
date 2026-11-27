import { Routes } from "@angular/router";
import { resolveFavoriteTypes } from "@geexcode/geex-extensions-favorites";

import { MyProfilePage } from "./my-profile.page";

export const meRoutes: Routes = [{ path: "", component: MyProfilePage, resolve: { favoriteTypes: resolveFavoriteTypes } }];
