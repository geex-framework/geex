export const toFixed = (n: string | number, fixed: number = 2) => {
  let value = n;
  if (typeof value === "string") {
    value = Number(n);
  }
  return ~~(Math.pow(10, fixed) * value) / Math.pow(10, fixed);
};
