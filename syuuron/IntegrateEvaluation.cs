using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syuuron {
    
    public class IntegrateEvaluation {
        
        string ADDINNAME = "統合評価";

        // アイコンに表示される内容
        public object EA_GetMenuItems(EA.Repository Repository, string MenuLocation, String MenuName) {
            return ADDINNAME;
        }


        // 評価がクリックされたときの動作
        public void EA_MenuClick(EA.Repository Repository, string MenuLocation, string MenuName, string ItemName) {
            return;
        }
    }
}