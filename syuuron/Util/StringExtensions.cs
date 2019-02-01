using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace syuuron.Util {
    static class StringExtensions {
        
        public static int ToInt(this string str) {
            return int.Parse(str);
        }
        
        public static double ToDouble(this string str) {
            return double.Parse(str);
        }
        
        public static string str(this string str) {
            return str;
        }
        
        public static XDocument ToXDocument(this string str) {
            try {
                return XDocument.Parse(System.Web.HttpUtility.HtmlDecode(str));
            }
            catch {
                throw new Exception("以下のXMLstring変換時に例外が発生しました" 
                                    + Environment.NewLine 
                                    + str);
            }
        }

        public static bool ToBoolean(this string str) {
            return Convert.ToBoolean(str);
        }

        //ギガ,メガバイトをキロバイトに変換
        public static double ToKByte(this string str) {
            double number = Regex.Replace(str, @"[^0-9.]", "").ToDouble();
            string unit = str.ToLower();
            double cofficient = 1;
            if (unit.Contains("g")) cofficient = 1000000;
            else if (unit.Contains("m")) cofficient = 1000;
            else if (unit.Contains("k")) cofficient = 1;
            else cofficient = 0.001;
            return cofficient * number;
        }
    }
}
