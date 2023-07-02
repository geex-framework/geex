import { assertIsArray, assertIsNotArray } from "@/shared/extensions";
import { Component, EventEmitter, Input, Output } from "@angular/core";
import { BarSeriesOption, ECharts, EChartsOption, LineSeriesOption, PieSeriesOption } from "echarts";
import * as _ from "lodash";
import { BehaviorSubject } from "rxjs";
import { debounceTime } from "rxjs/operators";

import {
  DEFAULT_BAR_SERIES_OPTION,
  DEFAULT_CATEGORY_AXIS_CONFIG,
  DEFAULT_CHART_OPTION,
  DEFAULT_LINE_SERIES_OPTION,
  DEFAULT_PIE_SERIES_OPTION,
  DEFAULT_VALUE_AXIS_CONFIG,
} from "./default-option";

export interface EchartsEvent {
  // type of the component to which the clicked glyph belongs
  // i.e., 'series', 'markLine', 'markPoint', 'timeLine'
  componentType: "series" | "markLine" | "markPoint" | "timeLine";
  // series type (make sense when componentType is 'series')
  // i.e., 'line', 'bar', 'pie'
  seriesType: "line" | "bar" | "pie";
  // series index in incoming option.series (make sense when componentType is 'series')
  seriesIndex?: number;
  // series name (make sense when componentType is 'series')
  seriesName?: string;
  // data name, category name
  name?: string;
  // data index in incoming data array
  dataIndex?: number;
  // incoming rwa data item
  data?: Object;
  // Some series, such as sankey or graph, maintains more than
  dataType?: string;
  // incoming data value
  value?: number | number[];
  // color of component (make sense when componentType is 'series')
  color?: string;
  // User info (only available in graphic component
  // and custom series, if element option has info
  // property, e.g., {type: 'circle', info: {some: 123}})
  info?: any;
}
type PieDataDto = {
  name: string;
  value: number;
  [key: string]: any;
};
export type EchartsMouseEventType =
  | "chartClick"
  | "chartDblClick"
  | "chartMouseDown"
  | "chartMouseMove"
  | "chartMouseUp"
  | "chartMouseOver"
  | "chartMouseOut"
  | "chartGlobalOut"
  | "chartContextMenu";

export type SimpleLineSeriesConfig = Pick<LineSeriesOption, "smooth" | "name" | "label" | "areaStyle" | "yAxisIndex" | "markPoint"> & {
  data: Array<string | number>;
  type: "line";
  axisPointer: any;
};
export type SimpleBarSeriesConfig = Pick<BarSeriesOption, "stack" | "name" | "label" | "yAxisIndex" | "barGap" | "itemStyle"> & {
  data: Array<string | number>;
  type: "bar";
  axisPointer: any;
};
export type SimplePieSeriesConfig = Pick<PieSeriesOption, "name" | "label" | "radius" | "tooltip" | "itemStyle" | "emphasis"> & {
  data: PieDataDto[];
  type: "pie";
  graphic?: any[];
};

export type SimpleSeriesConfig = (SimpleLineSeriesConfig | SimpleBarSeriesConfig | SimplePieSeriesConfig) & {
  data: Array<string | number | PieDataDto>;
};

export interface SimpleSeries<T = SimpleSeriesConfig> {
  seriesConfig: T;
  isSummarySeries?: boolean;
}
// 值轴
export interface IValueAxisConfig {
  series: SimpleSeries[];
  unit?: string;
  alpha?: number;
  axisLine?: any;
  axisPointer?: any;
  splitLine?: any;
  axisTick?: any;
}
// 类目轴
export interface ICategoryAxisConfig {
  categories: string[];
  rotate?: number;
  axisLine?: any;
  axisPointer?: any;
  splitLine?: any;
}

export type IAxisConfig = IValueAxisConfig | ICategoryAxisConfig;
@Component({
  selector: "static-chart",
  templateUrl: "./static-chart.component.html",
})
export class StaticChartComponent {
  private chart: ECharts;
  private options$ = new BehaviorSubject<EChartsOption>(undefined);
  public options: EChartsOption = _.merge({}, DEFAULT_CHART_OPTION);
  // 保留几位小数
  @Input() decimalPlace = 2;
  /** 图表高度 */
  @Input() chartHeight: string;

  @Input()
  set chartTitle(value: string) {
    assertIsNotArray(this.options.title);
    this.options.title.text = value;
  }
  @Input()
  set subTitle(value: string) {
    assertIsNotArray(this.options.title);
    this.options.title.subtext = value;
  }
  @Input()
  set titleStyle(value: any) {
    if (!value) return;
    assertIsNotArray(this.options.title);
    _.merge(this.options.title["textStyle"], value);
  }
  @Input()
  set legendTextStyle(value: any) {
    if (!value) return;
    _.merge(this.options.legend["textStyle"], value);
  }
  @Input()
  set legendPageTextStyle(value: any) {
    if (!value) return;
    _.merge(this.options.legend["pageTextStyle"], value);
  }
  @Input() set dataZoom(value: any[]) {
    this.options.dataZoom = value;
  }
  // x轴配置
  @Input()
  set xAxisConfigs(axisConfigs: IAxisConfig[]) {
    this.setEChartsAxis(axisConfigs, "xAxis");
    // this.options$.next(this.options);
  }

  // y轴配置
  @Input()
  set yAxisConfigs(axisConfigs: IAxisConfig[]) {
    this.setEChartsAxis(axisConfigs, "yAxis");
    this.options$.next(this.options);
  }
  @Output() readonly nzChartClick = new EventEmitter();
  eventsList: Array<(param: EchartsEvent) => void> = [];
  chartInit($event) {
    this.chart = $event;
    this.options$.pipe(debounceTime(300)).subscribe(res => {
      this.chart.setOption(res, true);
    });
  }
  /**
   * @description 轴配置
   * @param axisConfigs IAxisConfig[]
   * @param axisName 轴名称 x/y
   */
  setEChartsAxis(axisConfigs: IAxisConfig[], axisName: "yAxis" | "xAxis"): void {
    if (!(axisConfigs && axisConfigs.any())) {
      return;
    }
    axisConfigs.forEach((item, index) => {
      let isCategory = "categories" in item;

      // 类目轴
      if (isCategory) {
        this.options[axisName][index] = _.merge({}, DEFAULT_CATEGORY_AXIS_CONFIG, {
          data: (item as ICategoryAxisConfig).categories,
          axisLabel: { rotate: (item as ICategoryAxisConfig).rotate },
          axisLine: (item as ICategoryAxisConfig).axisLine,
          axisPointer: (item as ICategoryAxisConfig).axisPointer,
        });
      } else {
        // 值轴
        this.options.series = [];
        let seriesList = (item as IValueAxisConfig).series?.map(x => x.seriesConfig);
        if (!seriesList || !seriesList.any()) {
          return;
        }

        seriesList.forEach((series, i) => {
          // 默认配置  // 1. 优先处理一下数据 （单位，进制换算，小数点位数）
          let seriesOpt;
          switch (series.type) {
            case "line":
              seriesOpt = DEFAULT_LINE_SERIES_OPTION;
              series.data = (series.data as number[]).map(x => this.computeData(x, item));
              break;
            case "bar":
              seriesOpt = DEFAULT_BAR_SERIES_OPTION;
              series.data = (series.data as number[]).map(x => this.computeData(x, item));
              break;
            case "pie":
              seriesOpt = DEFAULT_PIE_SERIES_OPTION;
              delete this.options.xAxis;
              delete this.options.yAxis;
              series.data = series.data
                .filter(y => y.value >= 0)
                .map(x => ({
                  ...x,
                  value: this.computeData(x.value, item),
                }));
              this.options.graphic = series.graphic;
              break;
            default:
              break;
          }

          this.options.series[i] = _.merge({}, seriesOpt, series);
        });
        if (this.options[axisName]) {
          this.options[axisName][index] = _.merge({}, DEFAULT_VALUE_AXIS_CONFIG, {
            axisLabel: {
              formatter: `{value}\u0020${(item as IValueAxisConfig).unit}`,
            },
            axisLine: (item as IValueAxisConfig).axisLine,
            axisPointer: (item as IValueAxisConfig)?.axisPointer,
            splitLine: (item as IValueAxisConfig)?.splitLine,
            axisTick: (item as IValueAxisConfig)?.axisTick,
          });
        }
      }
    });
  }
  computeData(value, item: IAxisConfig) {
    return Number((value * (item as IValueAxisConfig).alpha).toFixed(this.decimalPlace));
  }
  isCategoryAxisConfig(x: IAxisConfig): x is ICategoryAxisConfig {
    return (x as ICategoryAxisConfig).categories != undefined;
  }
  isValueAxisConfig(x: IAxisConfig): x is IValueAxisConfig {
    return (x as ICategoryAxisConfig).categories == undefined;
  }

  // 自动计算图表的默认高度
  setChartHeight(): string {
    const chartHeight = this.chartHeight ? this.chartHeight : document.body.clientHeight > 770 ? "400px" : "300px";
    return chartHeight;
  }
  ngOnDestroy(): void {
    this.options$.unsubscribe();
  }

  onChartEvent(event: any, eventType: EchartsMouseEventType) {
    if (eventType === "chartClick") {
      if (event.componentType == "series") {
        this.nzChartClick.emit(event);
      }
    }
  }
}
