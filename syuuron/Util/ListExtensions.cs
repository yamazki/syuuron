using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syuuron.Util {
    static class ListExtensions {
        
        // リストの先頭を削除し、その値をreturnする
        public static T Pop<T>(this IList<T> list) {
            var result = list.First();
            list.RemoveAt(0);
            return result;
        }
    }
}
