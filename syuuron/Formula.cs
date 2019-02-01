using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace syuuron {

    static class Formula {

        public static double ExecFormula(string TypeOfProcessing, double DataAmount, double Cpu, double Memory) {
            // 処理時間を返す,単位は秒(m)
            double result = 0;
            switch (TypeOfProcessing) {
                case ("画像認識処理"):
                    var formula = Math.Pow(Cpu, -1.0);
                    var Coefficient = 2.7 * Math.Pow(10, -3) * DataAmount;
                    result = Coefficient * formula / 1000;
                    break;
                case ("データ変形処理"):
                    result = 0.001 * DataAmount;
                    break;
                case ("データ取得処理"): case ("制御処理"): case("命令送信"):
                    result = 0;
                    break;
                default:
                    result = 0;
                    break;
            }
            //結果がmsなので1000で割ってsにする
            return result / 1000;
        }    

    }
}
