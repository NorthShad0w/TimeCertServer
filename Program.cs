﻿using System.Globalization;
using System.Net;
using System.Text;
using LibTimeStamp;

namespace FakeStamping
{
    class Program
    {
        static TSResponder? tsResponder;
        static readonly string listen_path = @"/TSA/";
        static readonly string listen_addr = @"*";
        static readonly string listen_port = @"8080";
        static readonly string server_full = @"http://time.pika.net.cn/fake/RSA/";
        static readonly string server_cert = @"certificate.pem";
        static readonly string server_keys = @"private.key";
        static readonly string supportFake = @"true";
        static void Main(string[] args)
        {
            PrintInfo();
            try
            {
                tsResponder = new TSResponder(File.ReadAllBytes((string)server_cert),
                                              File.ReadAllBytes((string)server_keys), "SHA1");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message: " + ex.Message);
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("   [Error!!] Can NOT Find TimeStamp Cert File");
                Console.WriteLine("   [Warning] Please Check Your Cert and Key!");
                return;
            }
            HttpListener listener = new HttpListener();
            try
            {
                listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                listener.Prefixes.Add(@"http://"+ listen_addr + ":"+ listen_port + listen_path);
                listener.Start();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Message: " + ex.Message);
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                //Console.ReadLine();
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   [Success] Your TimeStamp HTTP Server Started Successfully!");
            Console.WriteLine("   [Success] TimeStamp Responder: http://" + listen_addr + ":"+ listen_port + listen_path);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            while (true)
            {
                HttpListenerContext ctx = listener.GetContext();
                ThreadPool.QueueUserWorkItem(new WaitCallback(TaskProc), ctx);
            }
        }

        static void PrintInfo()
        {
            Console.WriteLine("For Example: http://your_ip:port/path/2020-01-01T00:00:00");
            Console.WriteLine("   [Message] Server Address: " + @"http://" + listen_addr + ":" + listen_port + listen_path);
            Console.WriteLine("   [Message] TimeStamp Cert File Name: " + server_cert);
            Console.WriteLine("   [Message] TimeStamp Keys File Name: " + server_keys);
        }
        static void TaskProc(object? o)
        {
            HttpListenerContext ctx;
            ctx = (HttpListenerContext)o;
            ctx.Response.StatusCode = 200;
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;
            if (ctx.Request.HttpMethod != "POST")
            {
                StreamWriter writer = new StreamWriter(response.OutputStream, Encoding.ASCII);
                
                writer.WriteLine("OK");
                writer.Close();
                ctx.Response.Close();
            }
            else
            {
                string log = "";
                string date = request.RawUrl?.Remove(0, listen_path.Length)?? "";
                DateTime signTime;
                signTime = DateTime.UtcNow;
                if (supportFake == "true")
                {
                    Console.WriteLine("   [Success] Fake Stamp Responder: " + supportFake);
                    if (!DateTime.TryParseExact(date, "yyyy-MM-dd'T'HH:mm:ss",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                                out signTime))
                    {
                        signTime = DateTime.UtcNow;
                        Console.WriteLine("   [Warning] Can Not Process Time: " + date);
                    }
                    else
                    {
                        Console.WriteLine("   [Success] Fake Stamp Responder: " + date);
                    }
                }
                else
                {
                    Console.WriteLine("   [Success] Real Stamp Responder!");
                }
                BinaryReader reader = new BinaryReader(request.InputStream);
                byte[] bRequest = reader.ReadBytes((int)request.ContentLength64);

                bool RFC;
                byte[] bResponse = tsResponder!.GenResponse(bRequest, signTime, out RFC);
                if (RFC)
                {
                    response.ContentType = "application/timestamp-reply";
                    log += "   [Success] RFC3161 Time Stamping ";
                }
                else
                {
                    response.ContentType = "application/octet-stream";
                    log += "   [Success] Authenticode Stamping ";
                }
                log += signTime;
                BinaryWriter writer = new BinaryWriter(response.OutputStream);
                writer.Write(bResponse);
                writer.Close();
                ctx.Response.Close();
                Console.WriteLine(log);
            }
        }
    }
}