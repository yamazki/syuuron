using EA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace syuuron {

    public class Device : DeployDiagramObject {

        public string DeviceType;
        public List<Connector> ConnectorList = new List<Connector>();

        public Device(string Name, int ID, int ParentID, XDocument xdocument, string DeviceType, List<Connector> ConnectorList) 
            : base(Name, ID, ParentID, xdocument) {
            this.DeviceType = DeviceType;
            this.ConnectorList = ConnectorList;
        } 




    }
}
