import { Component, OnInit } from "@angular/core";
import { Apollo, gql } from "apollo-angular";
@Component({
  selector: "layout-blank",
  template: `<router-outlet></router-outlet> `,
  // tslint:disable-next-line: no-host-metadata-property
  host: {
    "[class.alain-blank]": "true",
  },
})
export class LayoutBlankComponent {
  value: string;
}
