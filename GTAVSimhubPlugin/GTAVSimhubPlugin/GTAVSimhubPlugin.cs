﻿using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


/// <summary>
/// GTA V Simhub Plugin
/// 
/// If this code works, it has been written by Carlo Iovino (carlo.iovino@outlook.com)
/// The Green Dragon Youtube Channel (www.youtube.com/carloxofficial
/// 
/// </summary>
namespace GTAVSimhub.Plugin
{

    sealed class DataProducer : IDisposable
    {
        private BinaryFormatter binaryFormatter = new BinaryFormatter();
        private SharedMemory.SharedArray<byte> sharedBuffer = null;

        private byte[] ToBinary(Object source)
        {
            using (var ms = new MemoryStream())
            {
                binaryFormatter.Serialize(ms, source);
                ms.Flush();
                return ms.ToArray();
            }
        }

        public void Share(Object o)
        {
            byte[] rawData = ToBinary(o);
            int dataSize = rawData.Length;

            // Write the dataSize and the rawData into the shared buffer
            Byte[] buf = new Byte[4 + dataSize];
            Array.Copy(BitConverter.GetBytes(dataSize), buf, 4);
            Array.Copy(rawData, 0, buf, 4, rawData.Length);

            try
            {
                // Acquire the write lock
                sharedBuffer.AcquireWriteLock();
                // Write binary data
                sharedBuffer.Write(buf);
                // Release the write lock
                sharedBuffer.ReleaseWriteLock();
            }
            catch (TimeoutException e)
            {
                Console.Write(e.Message);
            }
        }

        // Class constructor
        public DataProducer(string memId)
        {
            try
            {
                sharedBuffer = new SharedMemory.SharedArray<byte>(name: memId);
            }
            catch (Exception e)
            {
                sharedBuffer = new SharedMemory.SharedArray<byte>(name: memId, length: 65535);
            }
        }


        #region IDisposable Support
        private bool disposedValue = false; // Per rilevare chiamate ridondanti

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: eliminare lo stato gestito (oggetti gestiti).
                    sharedBuffer.Dispose();
                }

                // TODO: liberare risorse non gestite (oggetti non gestiti) ed eseguire sotto l'override di un finalizzatore.
                // TODO: impostare campi di grandi dimensioni su Null.

                disposedValue = true;
            }
        }

        // TODO: eseguire l'override di un finalizzatore solo se Dispose(bool disposing) include il codice per liberare risorse non gestite.
        // ~Producer() {
        //   // Non modificare questo codice. Inserire il codice di pulizia in Dispose(bool disposing) sopra.
        //   Dispose(false);
        // }

        // Questo codice viene aggiunto per implementare in modo corretto il criterio Disposable.
        public void Dispose()
        {
            // Non modificare questo codice. Inserire il codice di pulizia in Dispose(bool disposing) sopra.
            Dispose(true);
            // TODO: rimuovere il commento dalla riga seguente se è stato eseguito l'override del finalizzatore.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    class GTAVSimHubClient : Script
    {
        DataProducer dataProducer;

        const string P_CURRENTGEAR = "GameData.NewData.Gear";
        const string P_SPEED = "GameData.NewData.SpeedKmh";
        const string P_RPMS = "GameData.NewData.Rpms";
        const string P_GAMEISRUNNING = "GameIsRunning";

        public GTAVSimHubClient()
        {
            dataProducer = new DataProducer("GTAVSimHubPlugin");
        }

        string dataRow(string property, object value)
        {
            string type;

            type = value.GetType().Name;
            string r = property + ":" + type + ":" + value.ToString();
            return r;
        }

        void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            List<string> dataList = new List<string>();            

            if (player.IsInVehicle())
            {
                // Player in vehicle
                Vehicle vehicle = player.CurrentVehicle;

                dataList.Add(dataRow(P_RPMS, Convert.ToDouble(vehicle.CurrentRPM)));
                dataList.Add(dataRow(P_SPEED, vehicle.Speed));
                dataList.Add(dataRow(P_CURRENTGEAR, vehicle.CurrentGear));                
            }
            else
            {
                dataList.Add(dataRow(P_RPMS, player.IsInCombat ? 100 : 0));
                dataList.Add(dataRow(P_SPEED, player.Health));
                dataList.Add(dataRow(P_CURRENTGEAR, 0));
            }

            // Share data
            string[] dataArray = dataList.ToArray();
            dataProducer.Share(dataArray);
        }
    }
}