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
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Extensions;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Sockets;
using WhatsSocket.Exceptions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WhatsSocketConsole
{
    internal class Program
    {

        static WASocket socket;
        static void Main(string[] args)
        {
            Tests.RunTests();



            //This creds file comes from the nodejs sample    





            var config = new SocketConfig()
            {
                ID = "27665245067",
            };


            var credsFile = Path.Join(config.CacheRoot, "creds.json");
            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();

            BaseKeyStore keys = new FileKeyStore(config.CacheRoot);

            config.Auth = new AuthenticationState()
            {
                Creds = authentication,
                Keys = keys
            };

            socket = new WASocket(config);


            var authEvent = socket.EV.On<AuthenticationCreds>(EmitType.Update);
            authEvent.Emit += AuthEvent_OnEmit;

            var connectionEvent = socket.EV.On<ConnectionState>(EmitType.Update);
            connectionEvent.Emit += ConnectionEvent_Emit;


            var messageEvent = socket.EV.On<MessageUpsertModel>(EmitType.Upsert);
            messageEvent.Emit += MessageEvent_Emit;

            var history = socket.EV.On<MessageHistoryModel>(EmitType.Set);
            history.Emit += History_Emit;

            var presence = socket.EV.On<PresenceModel>(EmitType.Update);
            presence.Emit += Presence_Emit;

            //socket.EV.OnCredsChange += Socket_OnCredentialsChangeArgs;
            //socket.EV.OnDisconnect += EV_OnDisconnect;
            //socket.EV.OnQR += EV_OnQR;
            //socket.EV.OnMessageUpserted += EV_OnMessageUpserted;


            socket.MakeSocket();

            Console.ReadLine();
        }

        private static void Presence_Emit(BaseSocket sender, PresenceModel[] args)
        {
            Console.WriteLine(JsonConvert.SerializeObject(args[0], Formatting.Indented));
        }

        private static void History_Emit(BaseSocket sender, MessageHistoryModel[] args)
        {
            messages.AddRange(args[0].Messages);
            var jsons = messages.Select(x => x.ToJson()).ToArray();
            var array = $"[\n{string.Join(",", jsons)}\n]";
            Debug.WriteLine(array);
        }

        static List<WebMessageInfo> messages = new List<WebMessageInfo>();

        private static async void MessageEvent_Emit(BaseSocket sender, MessageUpsertModel[] args)
        {
            messages.AddRange(args[0].Messages);
            var jsons = messages.Select(x => x.ToJson()).ToArray();
            var array = $"[\n{string.Join(",", jsons)}\n]";
            Debug.WriteLine(array);

            if (args[0]?.Messages[0]?.Message?.Conversation == "test")
            {
                var result = await socket.FetchStatus("27797798179@s.whatsapp.net");
            }
        }

        private static async void ConnectionEvent_Emit(BaseSocket sender, ConnectionState[] args)
        {
            var connection = args[0];
            Debug.WriteLine(JsonConvert.SerializeObject(connection, Formatting.Indented));
            if (connection.QR != null)
            {
                QRCodeGenerator QrGenerator = new QRCodeGenerator();
                QRCodeData QrCodeInfo = QrGenerator.CreateQrCode(connection.QR, QRCodeGenerator.ECCLevel.L);
                AsciiQRCode qrCode = new AsciiQRCode(QrCodeInfo);
                var data = qrCode.GetGraphic(1);
                Console.WriteLine(data);
            }
            if (connection.Connection == WAConnectionState.Close)
            {
                if (connection.LastDisconnect.Error is Boom boom && boom.Data?.StatusCode != (int)DisconnectReason.LoggedOut)
                {
                    try
                    {
                        Thread.Sleep(1000);
                        sender.MakeSocket();
                    }
                    catch (Exception)
                    {

                    }
                }
                else
                {
                    Console.WriteLine("You are logged out");
                }
            }


            if (connection.Connection == WAConnectionState.Open)
            {
                //var result = await socket.OnWhatsApp("27797798179");
            }
        }

        private static void AuthEvent_OnEmit(BaseSocket sender, AuthenticationCreds[] args)
        {
            var credsFile = Path.Join(sender.SocketConfig.CacheRoot, $"creds.json");
            var json = AuthenticationCreds.Serialize(args[0]);
            File.WriteAllText(credsFile, json);
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




    }
}
