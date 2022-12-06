
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GeospatialPlaces
{
    /// <summary>
    /// 住所から階数を推定する
    /// </summary>
    public class FloorFinder
    {
        readonly Regex regexUnderground = new Regex(@"(地下|B)([\d]+)[F階]($|\s)");
        readonly Regex regex = new Regex(@"([\d]+)[F階]($|\s)");

        /// <summary>
        /// 住所の階数部分をパースする
        /// 地下の場合はnullを返す
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public int? FindFloor(string address)
        {
            // 地下はnull (１階扱いする)
            var resultUnderground = regexUnderground.Match(address);
            if (resultUnderground.Success)
            {
                return null;
            }

            // 情報がなければnull
            var result = regex.Match(address);
            if (!result.Success)
            {
                return null;
            }

            // 階数文字列部分のキャプチャを取得
            // 全角数字もなぜか入ってくる。
            var numbersStr = result.Groups[1].Value;

            // 全角数字を半角数字にコンバートしてからパースする
            var fixedNumberChars = numbersStr.Select(c =>
            {
                if (c >= '０' && c <= '９')
                {
                    return (char) (c - '０' + '0');
                }
                return c;
            });
            var normalizedNumberStr = new string(fixedNumberChars.ToArray());
            int value = Int32.Parse(normalizedNumberStr);
            return value;
        }
    }
}