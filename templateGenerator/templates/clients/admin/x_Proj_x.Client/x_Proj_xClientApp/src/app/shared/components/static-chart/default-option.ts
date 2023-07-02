import { BarSeriesOption, EChartsOption } from "echarts";
import * as _ from "lodash";

const titleFontSize = 14;
const labelFontSize = 10;

export const DEFAULT_LINE_SERIES_OPTION = {
  type: "line",
  smooth: true,
  label: {
    formatter: ({ value }) => value.toFixed(2),
    show: true,
    fontSize: labelFontSize,
  },
};

export const DEFAULT_BAR_SERIES_OPTION = {
  type: "bar",
  barGap: "30%",
  barMaxWidth: "50%",
  label: {
    formatter: ({ value }) => value.toFixed(2),
    fontSize: labelFontSize,
    position: "insideTop",
    color: "white",
    textBorderWidth: 3,
    textBorderColor: "inherit",
  },
};

export const DEFAULT_SUMMARY_BAR_SERIES_OPTION = {
  ...DEFAULT_BAR_SERIES_OPTION,
  itemStyle: {
    borderType: "dashed",
  },
};

export const DEFAULT_PIE_SERIES_OPTION = {
  tooltip: {
    trigger: "item",
  },
  legend: {
    top: "5%",
    left: "center",
    height: 400,
  },
  graphic: [],
};
//
export const DEFAULT_CATEGORY_AXIS_CONFIG = {
  type: "category",
  axisLabel: {
    fontSize: labelFontSize,
    fontWeight: "normal",
    interval: 0, // 可以设置成 0 强制显示所有标签。
    rotate: 0,
  },
  // Grid 区域垂直分割线
  splitLine: {
    show: false,
    lineStyle: { type: "dotted" },
  },
  // 轴线的配置
  axisLine: {
    show: true,
    lineStyle: { color: "#666" },
  },
  axisPointer: {},
};
export const DEFAULT_VALUE_AXIS_CONFIG = {
  type: "value",
  name: "",
  position: "left",
  // Grid 区域水平分割线
  splitLine: {
    show: true,
    lineStyle: { type: "dotted" },
  },
  // 轴线的配置
  axisLine: {
    show: false,
    lineStyle: { color: "#666" },
  },
  // 值轴标签
  axisLabel: {
    show: true,
    formatter: "{value}",
    fontSize: labelFontSize,
  },
  // 值轴的刻度线
  axisTick: {
    show: false,
  },
  axisPointer: {},
};

const isMobile = navigator.userAgent.includes("Mobile");
export const DEFAULT_CHART_OPTION = {
  // 标题
  title: {
    text: "统计图",
    top: "5px",
    left: 20,
    textStyle: {
      fontSize: titleFontSize,
      fontWeight: "normal",
      color: "#6C6C6C",
    },
  },
  legend: {
    // 图例
    type: "scroll",
    orient: "vertical",
    pageIconColor: "#2f4554",
    pageIconSize: 10,
    height: 100,
    right: 10,
    top: 10,
    itemGap: 7,
    itemWidth: 15, // 图例标记的图形宽度
    itemHeight: 10,
    textStyle: {
      fontSize: 10,
      fontWeight: "normal",
    },
    pageTextStyle: {},
  },
  grid: [{ left: isMobile ? 60 : "10%" }, { bottom: "12%" }, { top: "10%" }, { right: "10%" }],
  xAxis: [DEFAULT_CATEGORY_AXIS_CONFIG],
  yAxis: [DEFAULT_VALUE_AXIS_CONFIG],
  tooltip: {
    // 提示框
    show: true,
    axisPointer: {
      type: "cross",
    },
    trigger: "axis",
  },
  series: [], // 动态添加
} as EChartsOption;
