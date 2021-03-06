﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MS.Internal.Ink;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FakePacketSender.FakePacket
{
    public class FakePacket
        : BitStreamWriter
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate uint Send2(IntPtr packet);

        public int Opcode { get; private set; }
        private Process Process;
        private Send2 Send2Func;
        private IntPtr vTable;

        public FakePacket(int opcode)
            : base()
        {
            this.Opcode = opcode;
            this.WriteInt32(this.Opcode);

            //init delegates
            this.Process = Process.GetCurrentProcess();

            this.Send2Func = Marshal.GetDelegateForFunctionPointer(
                IntPtr.Add(Process.MainModule.BaseAddress, App.Offsets.Send2),
                typeof(Send2)) as Send2;

            if (Send2Func == null)
                throw new Exception("Can't create delegate \"Send2\"!");

            vTable = IntPtr.Add(Process.MainModule.BaseAddress, App.Offsets.VTable);
        }

        public void Clear()
        {
            this.Buffer.Clear();
            this.WriteInt32(this.Opcode);
            this.Flush();
        }

        public void WriteBits(uint value, int count)
        {
            this.Write(value, count);
        }

        public void WriteInt32(int value)
        {
            this.Flush();
            this.Buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void WriteFloat(float value)
        {
            this.Flush();
            this.Buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void WriteBytes(params byte[] bytes)
        {
            this.Flush();
            this.Buffer.AddRange(bytes);
        }

        public unsafe void Send()
        {
            fixed (byte* bytes = this.Buffer.ToArray())
            {
                var packet = new CDataStore((void*)vTable, bytes, this.Buffer.Count);

                Debug.WriteLine(string.Join(" ", this.Buffer.Select(n => n.ToString("X02"))));

                var packetLen = Marshal.SizeOf(typeof(CDataStore));
                var packetPtr = Marshal.AllocHGlobal(packetLen);
                Marshal.StructureToPtr(packet, packetPtr, true);

                try
                {
                    Send2Func(packetPtr);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Marshal.FreeHGlobal(packetPtr);
                }
            }
        }

        public static FakePacket CreateFakePacket(int opcode)
        {
            return new FakePacket(opcode);
        }
    }
}