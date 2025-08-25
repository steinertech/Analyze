export class UtilClient{
  public static versionClient = '1.0.16'

  public static MathFloor100(value: number) {
    // Also be aware of 100.00 - 80.04 = 19.959999999999994
    return Math.floor(value * 100) / 100
  }

  public static MathRound100(value: number) {
    return Math.round(value * 100) / 100
  }
}