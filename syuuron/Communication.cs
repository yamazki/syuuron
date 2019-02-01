using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace syuuron {
    public class Communication : DeployDiagramObject {
        public Communication(string Name, int ID, int ParentID, XDocument xdocument)
        : base(Name, ID, ParentID, xdocument) { }
    }
}

