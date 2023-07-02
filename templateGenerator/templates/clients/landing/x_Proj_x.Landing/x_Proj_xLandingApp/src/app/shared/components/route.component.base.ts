import { Observable } from "rxjs";

export abstract class RoutedComponentStore<TMetaData = any, TData = any> extends ComponentStore<{ metaData: TMetaData; data: TData }> {}
