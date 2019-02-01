using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syuuron {
    
     /*
     string Name 通信の種類の名前
     int Speed 通信の速度(kbps)
     enum Distance 通信距離(1 -> 近距離, 2 -> 遠距離)
     int Power 消費電力 1 ~ 5
     */
     public class CommunicationTypeInfo {
        
         public string Name { get; set; }
         public string Speed { get; set; }
         public int Distance { get; set; }
         public int PowerSaving { get; set; }
        
         static private List<CommunicationTypeInfo> CommunicationInfo = new List<CommunicationTypeInfo> {
             
             new CommunicationTypeInfo {
                 // zigbee等
                 Name = "近距離低速通信",
                 Speed = "250kbps",
                 Distance = 1,
                 PowerSaving = 5,
             },
             
             new CommunicationTypeInfo {
                 // BLT等
                 Name = "近距離中速通信",
                 Speed = "2000kbps",
                 Distance = 1,
                 PowerSaving = 3,
             },
             
             new CommunicationTypeInfo {
                 // Wi-Fi等, IEEE802.11ac参考,アンテナ1本
                 Name = "近距離高速通信",
                 Speed = "150Mbps",
                 Distance = 1,
                 PowerSaving = 1,
             },
             
             new CommunicationTypeInfo {
                 // LoRa等
                 Name = "遠距離低速通信",
                 Speed = "250Mbps",
                 Distance = 2,
                 PowerSaving = 5,
             },
             
             new CommunicationTypeInfo {
                 // LTE等
                 Name = "遠距離高速通信",
                 Speed = "300Mbps",
                 Distance = 2,
                 PowerSaving = 1,
             },
             
             new CommunicationTypeInfo {
                 Name = "有線通信",
                 Speed = "300Mbps",
                 Distance = 1,
                 PowerSaving = 2,
             },
         };
        
        static private Dictionary<string, CommunicationTypeInfo> CommunicationInfoDictionary = CommunicationInfo.ToDictionary(CommunicationTypeInfo => CommunicationTypeInfo.Name);

        static public CommunicationTypeInfo GetCommunicationInfo (string CommunicatonType) => CommunicationInfoDictionary[CommunicatonType];
    }
}
