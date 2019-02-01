using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using syuuron.Util;

namespace syuuron {

    public class RecommendMain {
        
        public Dictionary<string, bool> CommunicationMoveableDictionary = new Dictionary<string, bool>();
        
        public object EA_GetMenuItems(EA.Repository Repository, string MenuLocation, String MenuName) {
            return "推薦";
        }

        public void EA_MenuClick(EA.Repository Repository, string MenuLocation, string MenuName, string ItemName) {
            var Evaluation = new EvaluationMain();
            Evaluation.LoadDeployDiagramObjectsInformation(Repository);
            var DeviceList = Evaluation.DeviceList;
            var CommunicationList = Evaluation.CommunicationList;
            var RecommendDeviceXDocuments = LoadRecommendXDocuments($@"{GlobalVar.ProjectPath}\RecommendDeviceXML\");
            var RecommendCommunicationXDocuments = LoadRecommendXDocuments($@"{GlobalVar.ProjectPath}\RecommendCommunicationXML\");
            LoadCommunicationMoveable();
            //CommunicationMoveableDictionary.ToList().ForEach(x => MessageBox.Show(x.ToString()));

            MakeRecommendDeviceXDocumentList(DeviceList, RecommendDeviceXDocuments).ToList()
                                                                                   .ForEach(xdocument => xdocument.Save($@"{GlobalVar.ProjectPath}\RecommendationResult\{xdocument.Element("recommendList").Element("targetDeviceName").Value}.xml")); 
            MakeRecommendCommunicationXDocumentList(CommunicationList, RecommendCommunicationXDocuments).ToList()
                                                                                                        .ForEach(xdocument => xdocument.Save($@"{GlobalVar.ProjectPath}\RecommendationResult\{xdocument.Element("recommendList").Element("targetCommunicationName").Value}.xml"));
            CommunicationMoveableDictionary.Clear();
        }


        // 通信が存在する環境が移動するかどうかの情報をクラス変数CommunicationMoveableDictionaryに突っ込む(重複なし)
        // 評価アドインの結果ファイルから情報を読み取るので注意
        internal void LoadCommunicationMoveable() {
            var xmlFiles = Directory.EnumerateFiles($@"{GlobalVar.ProjectPath}\EvaluationResult\");
            foreach (var xmlFile in xmlFiles) {
                var EvaluatedXDocumentOfFunction = XDocument.Load($@"{xmlFile}");
                var ProcessList = EvaluatedXDocumentOfFunction.Elements("function").Elements("processList").Elements("process");
                foreach (var Process in ProcessList) {
                    var isCommunication = Process.Element("communication").Value.ToBoolean();
                    var CommunicationName = Process.Element("name").Value;
                    if (isCommunication && !CommunicationMoveableDictionary.ContainsKey(CommunicationName)) {
                        CommunicationMoveableDictionary.Add(CommunicationName, Process.Element("movement").Value.ToBoolean());
                    }
                }
            }
        }

        internal IEnumerable<XDocument> LoadRecommendXDocuments(string  DirectroyPath) {
            var RecommendDeviceXmlFileList = Directory.EnumerateFiles(DirectroyPath, "*.xml", SearchOption.AllDirectories);
            foreach(var path in RecommendDeviceXmlFileList) {
                yield return XDocument.Load(@path);
            }
        }

        internal IEnumerable<XDocument> ExcludeNotMeetingRequiementsOfDevice(XDocument DeviceXDocument, IEnumerable<XDocument> RecommendDeivceXDocumentList) {
            foreach (var RecommendDeviceXDocument in RecommendDeivceXDocumentList) {
                // スペックを満たすかどうか
                if (RecommendDeviceXDocument.Element("device").Element("memory").Value.ToKByte() >= DeviceXDocument.Element("data").Element("デバイスのメモリ").Value.ToKByte() &&
                   RecommendDeviceXDocument.Element("device").Element("cpu").Value.ToKByte() >= DeviceXDocument.Element("data").Element("デバイスのCPU").Value.ToKByte()) {
                        yield return RecommendDeviceXDocument;
                }
            }
        }
        
        internal IEnumerable<XDocument> ExcludeNotMeetingRequiementsOfCommunication(XDocument CommunicationXDocument, IEnumerable<XDocument> RecommendCommunicationXDocumentList) {
            foreach (var RecommendCommunicationXDocument in RecommendCommunicationXDocumentList) {
                var CommunicationName = CommunicationXDocument.Element("data").Element("名前").Value;
                if (!CommunicationMoveableDictionary.ContainsKey(CommunicationName)) {
                    if (RecommendCommunicationXDocument.Element("communication").Element("type").Value == CommunicationXDocument.Element("data").Element("通信の種類").Value ) {
                        yield return RecommendCommunicationXDocument;
                    }
                    yield break;
                }
                if (RecommendCommunicationXDocument.Element("communication").Element("type").Value == CommunicationXDocument.Element("data").Element("通信の種類").Value ) {
                    // 通信をするデバイスが移動しない場合
                    if (!CommunicationMoveableDictionary[CommunicationName]) {
                        yield return RecommendCommunicationXDocument;
                    }
                    // 通信をするデバイスが移動する場合
                    else if (CommunicationMoveableDictionary[CommunicationName] == RecommendCommunicationXDocument.Element("communication").Element("roaming").Value.ToBoolean()) {
                        yield return RecommendCommunicationXDocument;
                    }
                }
            }
        }

        internal IEnumerable<XDocument> MakeRecommendDeviceXDocumentList(List<Device> DeviceList, IEnumerable<XDocument> RecommendDeviceXDocumentList) {
            foreach (var device in DeviceList) {
                if (device.Name == "クラウド") continue;
                var RecommendXmlStringBuilder = new StringBuilder("<recommendList>")
                                               .Append("<targetDeviceName>").Append(device.Name).Append("</targetDeviceName>")
                                               .Append("<type>").Append("device").Append("</type>")
                                               .Append("<recommendDeviceList>");
                foreach (var RecommendedDeviceXDocument in ExcludeNotMeetingRequiementsOfDevice(device.xdocument, RecommendDeviceXDocumentList)) {
                    RecommendXmlStringBuilder.Append("<recommendDevice>")
                                             .Append("<name>").Append(RecommendedDeviceXDocument.Element("device").Element("name").Value).Append("</name>")
                                             .Append("<cpu>").Append(RecommendedDeviceXDocument.Element("device").Element("cpu").Value).Append("</cpu>")
                                             .Append("<memory>").Append(RecommendedDeviceXDocument.Element("device").Element("memory").Value).Append("</memory>")
                                             .Append("<disk>").Append(RecommendedDeviceXDocument.Element("device").Element("disk").Value).Append("</disk>")
                                             .Append("<remarks>").Append(RecommendedDeviceXDocument.Element("device").Element("remarks").Value).Append("</remarks>")
                                             .Append("</recommendDevice>");
                }
                RecommendXmlStringBuilder.Append("</recommendDeviceList>");
                yield return RecommendXmlStringBuilder.Append("</recommendList>").ToString().ToXDocument();
            }
            
        }
        
        internal IEnumerable<XDocument> MakeRecommendCommunicationXDocumentList(List<Communication> CommunicationList, IEnumerable<XDocument> RecommendCommunicationXDocumentList) {
            foreach (var communication in CommunicationList) {
                var RecommendXmlStringBuilder = new StringBuilder("<recommendList>")
                                               .Append("<targetCommunicationName>").Append(communication.Name).Append("</targetCommunicationName>")
                                               .Append("<type>").Append("communication").Append("</type>")
                                               .Append("<recommendCommunicationList>");
                foreach (var RecommendCommunicationXdocument in ExcludeNotMeetingRequiementsOfCommunication(communication.xdocument, RecommendCommunicationXDocumentList)) {
                    RecommendXmlStringBuilder.Append("<recommendCommunication>")
                                             .Append("<name>").Append(RecommendCommunicationXdocument.Element("communication").Element("name").Value).Append("</name>")
                                             .Append("<type>").Append(RecommendCommunicationXdocument.Element("communication").Element("type").Value).Append("</type>")
                                             .Append("<frequency>").Append(RecommendCommunicationXdocument.Element("communication").Element("frequency").Value).Append("</frequency>")
                                             .Append("<distance>").Append(RecommendCommunicationXdocument.Element("communication").Element("distance").Value).Append("</distance>")
                                             .Append("<speed>").Append(RecommendCommunicationXdocument.Element("communication").Element("speed").Value).Append("</speed>")
                                             .Append("<topology>").Append(RecommendCommunicationXdocument.Element("communication").Element("topology").Value).Append("</topology>")
                                             .Append("<connections>").Append(RecommendCommunicationXdocument.Element("communication").Element("connections").Value).Append("</connections>")
                                             .Append("<powerSaving>").Append(6 - RecommendCommunicationXdocument.Element("communication").Element("powersaving").Value.ToDouble()).Append("</powerSaving>")
                                             .Append("<remarks>").Append(RecommendCommunicationXdocument.Element("communication").Element("remarks").Value).Append("</remarks>")
                                             .Append("</recommendCommunication>");
                }
                RecommendXmlStringBuilder.Append("</recommendCommunicationList>");
                yield return RecommendXmlStringBuilder.Append("</recommendList>").ToString().ToXDocument();
            }
            
        }
        
    }
}
