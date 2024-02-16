﻿using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Proto;
using QRCoder;
using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using WhatsSocket.Core;
using WhatsSocket.Core.Curve;
using WhatsSocket.Core.Events;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Stores;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.Models;

namespace WhatsSocketConsole
{
    internal class Program
    {

        static BaseSocket socket;
        static void Main(string[] args)
        {
            //Tests.RunTests();

    

            //This creds file comes from the nodejs sample    
            var credsFile = Path.Join(Directory.GetCurrentDirectory(), "test", "creds.json");


            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();



            socket = new BaseSocket("test", authentication, new Logger());


            socket.EV.OnCredsChange += Socket_OnCredentialsChangeArgs;
            socket.EV.OnDisconnect += EV_OnDisconnect;
            socket.EV.OnKeyStoreChange += EV_OnKeyStoreChange;
            socket.EV.OnSessionStoreChange += EV_OnSessionStoreChange;
            socket.EV.OnQR += EV_OnQR;
            socket.EV.OnContactChange += EV_OnContactChange;


            socket.MakeSocket();

            Console.ReadLine();
        }

        private static void EV_OnContactChange(BaseSocket sender, Contact args)
        {

        }

        private static void EV_OnQR(BaseSocket sender, QRData args)
        {

            QRCodeGenerator QrGenerator = new QRCodeGenerator();
            QRCodeData QrCodeInfo = QrGenerator.CreateQrCode(args.Data, QRCodeGenerator.ECCLevel.L);
            AsciiQRCode qrCode = new AsciiQRCode(QrCodeInfo);
            var data = qrCode.GetGraphic(1);

            Console.WriteLine(data);
        }

        private static void EV_OnSessionStoreChange(BaseSocket sender, SessionStore args)
        {

        }

        private static void EV_OnKeyStoreChange(BaseSocket sender, KeyStore args)
        {

        }



        private static void EV_OnDisconnect(BaseSocket sender, DisconnectReason args)
        {
            if (args != DisconnectReason.LoggedOut)
            {
                sender.MakeSocket();
            }
            else
            {
                Directory.Delete(Path.Join(Directory.GetCurrentDirectory(), "test"), true);
                sender.NewAuth();
                sender.MakeSocket();
            }
        }



        private static void Socket_OnCredentialsChangeArgs(BaseSocket sender, AuthenticationCreds authenticationCreds)
        {
            var credsFile = Path.Join(Directory.GetCurrentDirectory(), "test", $"creds.json");
            var json = AuthenticationCreds.Serialize(authenticationCreds);
            File.WriteAllText(credsFile, json);
        }


    }
}