using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace prologue_loader
{
    internal static class Program
    {
        const string prog_name = "Velocity Prolouge Loader";
        const string game_exe = "Prologue.exe";

        const uint asm_start = 0x02000000;
        const uint asm_end = 0x03000000;

        static byte[] protection_data = {
            0x00, 0x00, //je (last 2 byte)
            0x81, 0xEC, 0x08, 0x00, 0x00, 0x00, //sub esp, 00000008
            0x8D, 0x45, 0xF0, //lea eax, [ebp-10]
            0x89, 0x04, 0x24, //mov esp, [eax]
            0xE8 //call (first byte)
        };
        static byte[] mod_data = { 0x85 }; //jne (2nd byte)

        static void Main(string[] args)
        {
            try
            {
                if (!File.Exists(game_exe))
                {
                    MessageBox.Show(game_exe + " not found!", prog_name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                MemoryEdit.Memory mem = new MemoryEdit.Memory();
                Process game = Process.Start(game_exe, string.Join(" ", args));
                game.ProcessorAffinity = (IntPtr)1;
                //The dynamic assembly code may not load instantly
                Thread.Sleep(1000);
                mem.Attach((uint)game.Id, MemoryEdit.Memory.ProcessAccessFlags.All);
                for (uint addr = asm_start; addr < asm_end; addr++)
                {
                    byte[] tmp = mem.ReadBytes(addr, protection_data.Length);
                    if (Enumerable.SequenceEqual(tmp, protection_data))
                    {
                        mem.WriteBytes(addr - 3, mod_data, 1);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, prog_name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
