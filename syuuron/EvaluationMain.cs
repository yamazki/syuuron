using EA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using syuuron.Util;

namespace syuuron
{

    public class EvaluationMain
    {

        private string ADDINNAME = "評価";
        public List<Device> DeviceList { get; } = new List<Device>();
        public List<Component> ComponentList { get; } = new List<Component>();
        public List<Node> NodeList { get; } = new List<Node>();
        public List<ExecutionEnvironment> ExecutionEnvironmentList { get; } = new List<ExecutionEnvironment>();
        public List<Communication> CommunicationList { get; } = new List<Communication>();
        public List<XDocument> FunctionComposedXDocumentList { get; } = new List<XDocument>();
        public List<XDocument> DeviceCharacteristicXDocumentList { get; } = new List<XDocument>();
        public Repository Repository;

        // アイコンに表示される内容
        public object EA_GetMenuItems(EA.Repository Repository, string MenuLocation, String MenuName) {
            return ADDINNAME;
        }

        // 評価がクリックされたときの動作
        public void EA_MenuClick(EA.Repository Repository, string MenuLocation, string MenuName, string ItemName) {

            EA.Diagram diagram = Repository.GetCurrentDiagram();
            this.Repository = Repository;

            if (diagram == null) {
                MessageBox.Show("ダイアグラムを開いてから実行してください。", ADDINNAME);
                return;
            }

            else if (!diagram.Type.Equals("Deployment")) {
                MessageBox.Show("配置図を開いてから実行してください", ADDINNAME);
                return;
            }

            LoadDeployDiagramObjectsInformation(Repository);

            var InputXDocument = XDocument.Load($@"{GlobalVar.ProjectPath}\inputdata.xml");
            var ProcesseComposedComponentNameList = InputXDocument.Element("inputData").Element("functionLists").Elements("function");
            var FunctionComposedComponentLists = ConvertFunctionComposedComponentNameListsIntoFunctionComposedComponentLists(ProcesseComposedComponentNameList);
            var FunctionNameList = InputXDocument.Element("inputData").Elements("functionLists").Elements("function").Elements("functionName").ToList();

            foreach (var FunctionComposedComponentList in FunctionComposedComponentLists) {
                var FunctionName = FunctionNameList.Pop().Value;
                var FunctionComposedXDocument = GetFunctionComposedXDocument(FunctionComposedComponentList, FunctionName);
                // MessageBox.Show(FunctionComposedXDocument.ToString());
                FunctionComposedXDocument.Save($@"{GlobalVar.ProjectPath}\EvaluationResult\{FunctionName}.xml");
            }

            //MessageBox.Show("end");
            MakeDeviceQualityXDocuments(Repository);
            Initialize();
            return;
        }

        // コンポーネントの名前リストを受け取り、そのコンポーネントの名前をもつコンポーネントのリストを返す
        internal IEnumerable<Component> ConvertFunctionComposedComponentNameListIntoFunctionComposedComponentList(IEnumerable<String> ComponentNameList) {
            foreach (var ComponentName in ComponentNameList) {
                yield return ComponentList.Single(Component => Component.Name == ComponentName);
            }
        }

        internal IEnumerable<IEnumerable<Component>> ConvertFunctionComposedComponentNameListsIntoFunctionComposedComponentLists(IEnumerable<XElement> FunctionList) {
            foreach (var Function in FunctionList) {
                var ComponentNameList = Function.Elements("component")
                                                .Select(Component => Component.Value);
                yield return ConvertFunctionComposedComponentNameListIntoFunctionComposedComponentList(ComponentNameList);
            }
        }

        // DeviceのXdocumentから品質評価をしたDeviceのXdocumentを生成
        internal void MakeDeviceQualityXDocuments(Repository Repository) {
            double DeviceConfidentialitySum = 0;
            double DeviceRecoverySum = 0;
            double DeviceModularitySum = 0;
            double DeviceMaintainabilitySum = 0;
            int nCloud = 0;
            foreach (var device in DeviceList) {
                if (device.Name == "クラウド") {
                    nCloud += 1;
                    continue;     
                } 
                var DeviceConfidentility = EvaluateConfidentiality(Repository, device);
                var DeviceRecovery = EvaluateRecovery(Repository, device);
                var DeviceModularity = CalculateModularity(device, ComponentList);
                var DeviceMaintainability = EvaluateMaintainability(Repository, device);
                DeviceConfidentialitySum += DeviceConfidentility;
                DeviceRecoverySum += DeviceRecovery;
                DeviceModularitySum += DeviceModularity;
                DeviceMaintainabilitySum += DeviceMaintainability;
                var DeviceXDocumentStringBuilder = new StringBuilder()
                                                  .Append("<device>")
                                                  .Append("<deviceName>").Append(device.Name).Append("</deviceName>")
                                                  .Append("<confidentiality>").Append(DeviceConfidentility).Append("</confidentiality>")
                                                  .Append("<recovery>").Append(DeviceRecovery).Append("</recovery>")
                                                  .Append("<maintainability>").Append(DeviceMaintainability).Append("</maintainability>")
                                                  .Append("<modularity>").Append(DeviceModularity).Append("</modularity>")
                                                  .Append("</device>");
                var DeviceXml = DeviceXDocumentStringBuilder.ToString().ToXDocument();
                DeviceXml.Save($@"{GlobalVar.ProjectPath}\EachQualityEvaluationResult\{device.Name}.xml");
            }
            var Count = DeviceList.Count - nCloud;
            var QualityEvaluationXDocument = new StringBuilder()
                                            .Append("<quality>")
                                            .Append("<confidentiality>").Append(DeviceConfidentialitySum / Count).Append("</confidentiality>")
                                            .Append("<recovery>").Append(DeviceRecoverySum / Count).Append("</recovery>")
                                            .Append("<modularity>").Append(DeviceModularitySum / Count).Append("</modularity>")
                                            .Append("<maintainability>").Append(DeviceMaintainabilitySum / Count).Append("</maintainability>")
                                            .Append("</quality>");
            QualityEvaluationXDocument.ToString().ToXDocument().Save($@"{GlobalVar.ProjectPath}\ComprehensiveQualityEvaluationResult\quality.xml");
        }

        // 配置図の要素が属している実行環境の取得
        internal ExecutionEnvironment GetExecutionEnvironment(EA.Repository Repository, DeployDiagramObject DeployDiagramObject) {
            var diagram = Repository.GetCurrentDiagram();
            var diagramObject = diagram.GetDiagramObjectByID(DeployDiagramObject.ParentID, "");
            if (DeviceList.Where(Device => Device.ID == DeployDiagramObject.ParentID).Any()) {
                return GetExecutionEnvironment(Repository, DeviceList.Single(Device => Device.ID == DeployDiagramObject.ParentID));
            }
            return ExecutionEnvironmentList.Single(ExecutionEnvironment => ExecutionEnvironment.ID == DeployDiagramObject.ParentID);
        }

        // 図から必要な要素を取得し、リストを作成する
        internal void LoadDeployDiagramObjectsInformation(EA.Repository Repository) {
            
            // ノードの配置に関するモデルの作成,親をたどることにより必要な情報を入手
            // 配置図の要素を環境、デバイス、ノード、コンポーネントのリストに格納
            EA.Diagram diagram = Repository.GetCurrentDiagram();
            var TmpCommunicationList = new List<Communication>();
            for (short i = 0; i < diagram.DiagramObjects.Count; i++) {
                DiagramObject diagramObject = diagram.DiagramObjects.GetAt(i);
                Element element = Repository.GetElementByID(diagramObject.ElementID);
                var ConnectorList = new List<Connector>();
                var xdoc = element.Notes.ToXDocument();
                switch (element.MetaType) {
                    case "ExecutionEnvironment":
                        ExecutionEnvironmentList.Add(new ExecutionEnvironment(element.Name, element.ElementID, element.ParentID, xdoc));
                        break;

                    case "Device":
                        // デバイスが保持している接続を取得
                        for (short j = 0; j < element.Connectors.Count; j++) {
                            Connector connector = element.Connectors.GetAt(j);
                            ConnectorList.Add(connector);
                            TmpCommunicationList.Add(new Communication(connector.Name, connector.ConnectorID, connector.DiagramID, connector.Notes.ToXDocument()));
                        }

                        DeviceList.Add(new Device(element.Name, element.ElementID, element.ParentID, xdoc, element.Stereotype, ConnectorList));
                        break;

                    case "Node":
                        NodeList.Add(new Node(element.Name, element.ElementID, element.ParentID, xdoc));
                        break;

                    case "Component":
                        ComponentList.Add(new Component(element.Name, element.ElementID, element.ParentID, xdoc));
                        break;
                }
            }

            TmpCommunicationList.GroupBy(communication => communication.Name)
                                .Select(x => x.FirstOrDefault())
                                .ToList()
                                .ForEach(Communication => CommunicationList.Add(Communication));
        }

        internal void Initialize() {
            DeviceList.Clear();
            ComponentList.Clear();
            ExecutionEnvironmentList.Clear();
            NodeList.Clear();
        }

        // 親のデバイスを返す
        // コンポーネントもしくはデバイスを引数にする
        internal Device GetParentDevice(DeployDiagramObject DeployDiagramObject) {
            // MessageBox.Show(DeployDiagramObject.ParentID.ToString());
            // DeviceList.ForEach(Device => MessageBox.Show(Device.ID.ToString()));
            return DeviceList.Single(Device => Device.ID == DeployDiagramObject.ParentID);
        }

        // コンポーネント、もしくは通信からそれに対応する処理時間を計算したxmlを生成
        internal XDocument GetFunctionComposedXDocument(IEnumerable<Component> FunctionComposedComponent, string ProcessName) {
            var FunctionComposedXDocumentList = GetProcesstPath(FunctionComposedComponent.ToList());
            var ProcessingTimeList = MakeProcessingTimeList(FunctionComposedXDocumentList).ToList();
            StringBuilder ResultXDocumentStringBulider = new StringBuilder()
                                                        .Append("<function>")
                                                        .Append("<functionName>").Append(ProcessName).Append("</functionName>")
                                                        .Append("<processList>");
            double TotalProcessingTime = 0;
            while (FunctionComposedXDocumentList.Any()) {
                var FunctionComposedXDocument = FunctionComposedXDocumentList.Pop();
                var isCommunication = FunctionComposedXDocument.Element("communication") != null ? true : false;
                var ProcessXDocumentName = isCommunication
                                         ? FunctionComposedXDocument.Element("communication").Element("connector").Element("data").Element("名前").Value
                                         : FunctionComposedXDocument.Element("process").Element("component").Element("data").Element("名前").Value;
                var ProcessingTime = ProcessingTimeList.Pop();
                TotalProcessingTime += ProcessingTime;
                ResultXDocumentStringBulider.Append("<process>")
                                            .Append("<name>").Append(ProcessXDocumentName).Append("</name>")
                                            .Append("<processingTime>").Append(ProcessingTime).Append("</processingTime>")
                                            .Append("<communication>").Append(isCommunication).Append("</communication>");
                if (isCommunication) {
                    ResultXDocumentStringBulider.Append("<movement>").Append(FunctionComposedXDocument.Element("communication").Element("connector").Element("movement").Value).Append("</movement>");
                }
                ResultXDocumentStringBulider.Append("</process>");
            }
            ResultXDocumentStringBulider.Append("</processList>")
                                        .Append("<totalProcessingTime>").Append(TotalProcessingTime).Append("</totalProcessingTime>")
                                        .Append("</function>");
            return ResultXDocumentStringBulider.ToString().ToXDocument();
        }

        // ルートデバイスを返す
        internal Device GetRootDevice(DeployDiagramObject DeployDiagramObject) {
            // 親がExecutionEnvitomnentObjectなら現在のデバイスがルートデバイスなのでreturn
            if (ExecutionEnvironmentList.Any(ExecutionEnvironment => ExecutionEnvironment.ID == DeployDiagramObject.ParentID)) return DeployDiagramObject as Device;
            return GetRootDevice(DeviceList.Single(Device => Device.ID == DeployDiagramObject.ParentID));
        }

        // 機能を構成するコンポーネントをたどる関数
        // 機能を構成するコンポーネントのリストをたどり
        // 機能を構成するコンポーネントと通信の情報のリストを返す
        // やっていることは幅優先探索
        internal List<XDocument> GetProcesstPath(List<Component> FunctionComposedComponentList) {

            //FunctionComposedComponentList.ForEach(x => MessageBox.Show(x.Name));

            var CurrentComponent = FunctionComposedComponentList.Pop();
            var CurrentDevice = GetParentDevice(CurrentComponent);

            // MessageBox.Show(CurrentComponent.Name);

            // ComponentXMLにDeviceXMLを追加したxmlを生成
            // XMLを結合させるやり方がわからないので、文字列にして連結させている
            var CurrentComponentAndDeviceXDocumentStringBuilder = new StringBuilder()
                                                                 .Append("<process>")
                                                                 .Append("<component>").Append(CurrentComponent.xdocument).Append("</component>")
                                                                 .Append("<device>").Append(CurrentDevice.xdocument).Append("</device>")
                                                                 .Append("</process>");
            var CurrentComponentAndDeviceXDocument = CurrentComponentAndDeviceXDocumentStringBuilder.ToString().ToXDocument();
            FunctionComposedXDocumentList.Add(CurrentComponentAndDeviceXDocument);

            // 探索オブジェクトがなくなったら終了
            if (!FunctionComposedComponentList.Any()) {
                var result = new List<XDocument>(FunctionComposedXDocumentList);
                FunctionComposedXDocumentList.Clear();
                return result;
            }

            var NextComponent = FunctionComposedComponentList.First();

            // 探索ステップ1
            // 同一ルートデバイスに目的のコンポーネントが存在するか探索
            if (GetRootDevice(CurrentComponent) == GetRootDevice(NextComponent)) {
                return GetProcesstPath(FunctionComposedComponentList);
            }


            // 探索ステップ2
            // 目的のコンポーネントが別のデバイス上に存在している場合
            var SearchedConnectorList = new List<Connector>(GetRootDevice(CurrentDevice).ConnectorList);

            // 現在のデバイスとそのデバイスからの経路のXMLをもったDictionaryを作成
            // 初期要素は現在のデバイスとそのXML
            var DeviceAndPathXDocumentDictionary = new Dictionary<Device, List<XDocument>>() { { GetRootDevice(CurrentDevice), new List<XDocument>() } };

            while (SearchedConnectorList.Any()) {
                var connector = SearchedConnectorList.Pop();
                var connectorXDocument = connector.Notes.ToXDocument();
                var NextDevice = DeviceList.Single(Device => Device.ID == connector.SupplierID);
                var Movement = IsMovement(Repository, CurrentDevice, NextDevice);
                var ConnectorXDocumentStringBuilder = new StringBuilder()
                                                     .Append("<communication>")
                                                     .Append("<connector>")
                                                     .Append(connectorXDocument)
                                                     .Append("<movement>").Append(Movement).Append("</movement>")
                                                     .Append("</connector>")
                                                     .Append("<component>").Append(CurrentComponent.xdocument).Append("</component>")
                                                     .Append("</communication>");
                connectorXDocument = ConnectorXDocumentStringBuilder.ToString().ToXDocument();

                CurrentDevice = DeviceList.Single(Device => Device.ID == connector.ClientID);


                // 期待するSupplierIdとClientIdが逆だった場合に、これらを入れ替える
                if (!DeviceAndPathXDocumentDictionary.ContainsKey(CurrentDevice)) {
                    var tmp = CurrentDevice;
                    CurrentDevice = NextDevice;
                    NextDevice = tmp;
                }

                // 現在のルートデバイスから各ルートデバイスへの通信の経路XMLをもつリストを取得
                var ConnectorPathXDocumentList = new List<XDocument>(DeviceAndPathXDocumentDictionary[CurrentDevice]) { connectorXDocument };
                DeviceAndPathXDocumentDictionary.Add(NextDevice, ConnectorPathXDocumentList);

                // 探索したデバイスが目的のデバイスの場合、通信経路のXMLパスを経路情報に格納して再帰
                if (NextDevice == GetRootDevice(NextComponent)) {
                    ConnectorPathXDocumentList.ForEach(ConnectorPathXDocument => FunctionComposedXDocumentList.Add(ConnectorPathXDocument));
                    return GetProcesstPath(FunctionComposedComponentList);
                }

                // 探索したデバイスが目的のデバイスでなかった場合、探索したデバイスの他の接続情報を取得
                else if (NextDevice.ConnectorList.Count() > 1) {
                    NextDevice.ConnectorList.Where(Connector => Connector.Name != connector.Name)
                                            .ToList()
                                            .ForEach(Connector => SearchedConnectorList.Add(Connector));
                }
            }
            return null;
        }
          
        //環境が移動するかどうかを判定し、通信にローミング機能が必要か判定
        internal bool IsMovement(Repository Repository, Device CurrentDevice, Device NextDevice)  {
            var CurrentEnvitonement = GetExecutionEnvironment(Repository, CurrentDevice);
            var MoveCurrentEnvitonement = CurrentEnvitonement.xdocument.Element("data").Element("環境の移動性").Value.ToBoolean();
            if (!MoveCurrentEnvitonement) return false;
            var NextEnvironment = GetExecutionEnvironment(Repository, NextDevice);
            if (CurrentEnvitonement.Name == NextEnvironment.Name) return false;
            return true;
        }
        // 処理時間を計算したリストを返す
        internal IEnumerable<double> MakeProcessingTimeList(List<XDocument> ProcessList) {
            return ProcessList.Select(Process => {
                // MessageBox.Show(Process.ToString());
                return Process.Element("process") != null
                       ? CalculateProcessingTimeFromComponent(Process)
                       : CalculateProcessingTimeFromCommunication(Process);
            });
        }

        // コンポーネントの処理時間の計算
        internal double CalculateProcessingTimeFromComponent(XDocument Process) {
            string TypeOfProcessing = Process.Element("process").Element("component").Element("data").Element("処理の種類").Value;
            double DataAmount = Process.Element("process").Element("component").Element("data").Element("処理データ量").Value.ToKByte();
            double DeviceCpu = Process.Element("process").Element("device").Element("data").Element("デバイスのCPU").Value.ToKByte();
            double DeviceMemory = Process.Element("process").Element("device").Element("data").Element("デバイスのメモリ").Value.ToKByte();
            return Formula.ExecFormula(TypeOfProcessing, DataAmount, DeviceCpu, DeviceMemory);
        }

        // 通信の処理時間の計算
        internal double CalculateProcessingTimeFromCommunication(XDocument Process) {
            double CommunicationSpeed;
            if (Process.Element("communication").Element("connector").Element("data").Element("通信速度").Value.Length == 0) {
                var CommunicationType = Process.Element("communication").Element("connector").Element("data").Element("通信の種類").Value;
                var CommunicationInfo = CommunicationTypeInfo.GetCommunicationInfo(CommunicationType);
                CommunicationSpeed = CommunicationInfo.Speed.ToKByte();
            }
            else {
                CommunicationSpeed = Process.Element("communication").Element("connector").Element("data").Element("通信速度").Value.ToKByte();
            }
                
            double DataAmount = Process.Element("communication").Element("component").Element("data").Element("出力データ量").Value.ToKByte();

            //データ量(kb) / 通信速度(kbps)で求める
            return DataAmount / CommunicationSpeed;
        }

        // 機密性の評価
        internal double EvaluateConfidentiality(EA.Repository Repository, Device device) {
            var ExecutionEnvironmenWhereDeviceExists = GetExecutionEnvironment(Repository, device);
            double EnvironmenteConfidentiality = ExecutionEnvironmenWhereDeviceExists.xdocument.Element("data").Element("環境への認証アクセス性").Value.ToDouble();
            double DeviceConfidentiality = device.xdocument.Element("data").Element("デバイスへの認証アクセス性").Value.ToDouble();
            return CalculateConfidentiality(EnvironmenteConfidentiality, DeviceConfidentiality);
        }

        // 機密性の計算
        internal double CalculateConfidentiality(double EnvironmentAccesibility, double DeviceConfidentiality) {
            return (EnvironmentAccesibility + DeviceConfidentiality) / 2;
        }

        // 回復性の評価
        internal double EvaluateRecovery(EA.Repository Repository, Device device) {
            var ExecutionEnvironmenWhereDeviceExists = GetExecutionEnvironment(Repository, device);
            double EnvironmentAccesibility = ExecutionEnvironmenWhereDeviceExists.xdocument.Element("data").Element("環境へのアクセス性").Value.ToDouble();
            double DeviceAccesibility = device.xdocument.Element("data").Element("デバイスへのアクセス性").Value.ToDouble();
            double DevicerRepairability = device.xdocument.Element("data").Element("デバイスの修復性").Value.ToDouble();
            return CalculateRecovery(EnvironmentAccesibility, DeviceAccesibility, DevicerRepairability);
        }

        // 回復性の計算
        internal double CalculateRecovery(double EnvironmentAccesibility, double DeviceAccesibility, double DevicerRepairability) {
            return (EnvironmentAccesibility + DeviceAccesibility + DevicerRepairability) / 3;
        }

        // モジュール性の評価・計算
        // デバイスごとに評価・計算する
        internal double CalculateModularity(Device device, List<Component> Components) {
            int NumberOfComponentsOnDevice = Components.Where(Component => Component.ParentID == device.ID).Count();
            double Modularity = 5 - NumberOfComponentsOnDevice / 2;
            if (Modularity < 0) Modularity = 0;
            return Modularity;
        }


        public Dictionary<string, int> BatteryAbilityDictionary = new Dictionary<string, int>() {
            {"主電源", 1},
            {"電池", 2},
            {"充電池", 2},
            {"環境電池", 2},
        };
            
        // 保守性の計算・評価
        // ルートデバイスにのみ評価を適用する
        internal double EvaluateMaintainability(EA.Repository Repository, Device device) {

            // デバイスについている通信情報の取得
            double PowerForCommunication = 0; 
            foreach (var Connector in device.ConnectorList) {
                var ConnectorType = Connector.Notes.ToXDocument().Element("data").Element("通信の種類").Value;
                var CommunicationInfo = CommunicationTypeInfo.GetCommunicationInfo(ConnectorType);
                PowerForCommunication += (6 - CommunicationInfo.PowerSaving);
            }

            var ExecutionEnvironmenWhereDeviceExists = GetExecutionEnvironment(Repository, device);
            double EnvironmenteConfidentiality = ExecutionEnvironmenWhereDeviceExists.xdocument.Element("data").Element("環境へのアクセス性").Value.ToDouble();
            double DeviceConfidentiality = device.xdocument.Element("data").Element("デバイスへのアクセス性").Value.ToDouble();
            string DeviceBatteryType = device.xdocument.Element("data").Element("電源形式").Value;
            if (DeviceBatteryType == "主電源") PowerForCommunication = 0;
            
            // var BatteryAbility = BatteryAbilityDictionary[DeviceBatteryType];
            var UsingPower = PowerForCommunication;
            var Confidentiality = (EnvironmenteConfidentiality + DeviceConfidentiality) / 2;
            double Maintainability = 5 - ((5 - Confidentiality) * UsingPower);
            return Maintainability;
        }
    }
}