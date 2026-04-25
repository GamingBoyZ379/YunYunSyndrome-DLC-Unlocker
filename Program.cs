using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

class Program
{
    static void Main()
    {
        try
        {
            Console.Title = "Song Unlock Patcher";

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dll = Path.Combine(baseDir, @"Yunyun_Syndrome_Data\Managed\app.dll");

            Console.WriteLine("Base Folder:");
            Console.WriteLine(baseDir);
            Console.WriteLine();

            Console.WriteLine("Looking for:");
            Console.WriteLine(dll);
            Console.WriteLine();

            if (!File.Exists(dll))
            {
                Console.WriteLine("ERROR: app.dll not found.");
                Pause();
                return;
            }

            File.Copy(dll, dll + ".bak", true);

            byte[] data = File.ReadAllBytes(dll);
            var mod = ModuleDefMD.Load(data);

            bool patched = false;

            foreach (var type in mod.Types)
            {
                if (type.FullName == "App.Data.GlobalSaveData")
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.Name == "IsSongLocked")
                        {
                            method.Body = new CilBody();
                            method.Body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
                            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                            patched = true;
                        }
                    }
                }
            }

            if (patched)
            {
                mod.Write(dll);
                Console.WriteLine("Patch successful.");
            }
            else
            {
                Console.WriteLine("Method not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRASH:");
            Console.WriteLine(ex.ToString());
        }

        Pause();
    }

    static void Pause()
    {
        Console.WriteLine();
        Console.Write("Press any key...");
        Console.ReadKey();
    }
}