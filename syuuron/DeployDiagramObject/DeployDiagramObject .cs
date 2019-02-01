using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace syuuron { 

    public abstract class DeployDiagramObject {

        public string Name;
        public int ID;
        public int ParentID;
        public XDocument xdocument;


        public DeployDiagramObject(
            string Name,
            int ID,
            int ParentID,
            XDocument xdocument
        )
        { 
            this.Name = Name;
            this.ID = ID;
            this.ParentID = ParentID;
            this.xdocument = xdocument;
        }

        public void Show() {
            MessageBox.Show(Name + " " + ID + " " + ParentID);
        }
    }
}
