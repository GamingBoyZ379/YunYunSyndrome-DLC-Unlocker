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
            Console.Title = "DLC Unlocker";

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dll = Path.Combine(baseDir, @"Yunyun_Syndrome_Data\Managed\app.dll");

            if (!File.Exists(dll))
            {
                Console.WriteLine("ERROR: app.dll not found.");
                Console.WriteLine(dll);
                Pause();
                return;
            }

            File.Copy(dll, dll + ".bak", true);

            byte[] data = File.ReadAllBytes(dll);
            var mod = ModuleDefMD.Load(data);

            bool patchedSongs = false;
            bool patchedKeyboards = false;

            foreach (var type in mod.Types)
            {
                if (type.FullName == "App.Data.GlobalSaveData")
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.Name == "IsSongLocked")
                        {
                            Console.WriteLine("Patching songs...");
                            method.Body = new CilBody();
                            method.Body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
                            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                            patchedSongs = true;
                        }
                    }
                }

                if (type.FullName == "App.Data.SaveData")
                {
                    foreach (var nested in type.NestedTypes)
                    {
                        if (nested.Name.Contains("GetActiveKeyboard"))
                        {
                            foreach (var method in nested.Methods)
                            {
                                if (method.Name == "MoveNext" && method.HasBody)
                                {
                                    var stateField   = FindField(nested, "<>1__state");
                                    var currentField = FindField(nested, "<>2__current");

                                    if (stateField == null || currentField == null)
                                    {
                                        Console.WriteLine("ERROR: Could not find state machine fields on " + nested.Name);
                                        Console.WriteLine("Fields found:");
                                        foreach (var f in nested.Fields)
                                            Console.WriteLine("  " + f.Name);
                                        continue;
                                    }

                                    Console.WriteLine("Patching keyboards...");

                                    int[] keyboards = { 1, 2, 3, 4, 5, 6, 101, 102, 103, 104, 105 };

                                    var body = new CilBody();
                                    var instructions = body.Instructions;

                                    var yieldLabels = new Instruction[keyboards.Length];
                                    for (int i = 0; i < keyboards.Length; i++)
                                        yieldLabels[i] = new Instruction(OpCodes.Nop);

                                    var retFalse = new Instruction(OpCodes.Nop);

                                    instructions.Add(OpCodes.Ldarg_0.ToInstruction());
                                    instructions.Add(new Instruction(OpCodes.Ldfld, stateField));

                                    for (int i = 0; i < keyboards.Length; i++)
                                    {
                                        instructions.Add(OpCodes.Dup.ToInstruction());
                                        instructions.Add(new Instruction(OpCodes.Ldc_I4, i));
                                        instructions.Add(new Instruction(OpCodes.Beq, yieldLabels[i]));
                                    }

                                    instructions.Add(OpCodes.Pop.ToInstruction());
                                    instructions.Add(new Instruction(OpCodes.Br, retFalse));

                                    for (int i = 0; i < keyboards.Length; i++)
                                    {
                                        instructions.Add(yieldLabels[i]);
                                        instructions.Add(OpCodes.Pop.ToInstruction());

                                        instructions.Add(OpCodes.Ldarg_0.ToInstruction());
                                        instructions.Add(new Instruction(OpCodes.Ldc_I4, i + 1));
                                        instructions.Add(new Instruction(OpCodes.Stfld, stateField));

                                        instructions.Add(OpCodes.Ldarg_0.ToInstruction());
                                        instructions.Add(new Instruction(OpCodes.Ldc_I4, keyboards[i]));
                                        instructions.Add(new Instruction(OpCodes.Stfld, currentField));

                                        instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
                                        instructions.Add(OpCodes.Ret.ToInstruction());
                                    }

                                    instructions.Add(retFalse);

                                    instructions.Add(OpCodes.Ldarg_0.ToInstruction());
                                    instructions.Add(new Instruction(OpCodes.Ldc_I4, -1));
                                    instructions.Add(new Instruction(OpCodes.Stfld, stateField));

                                    instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
                                    instructions.Add(OpCodes.Ret.ToInstruction());

                                    method.Body = body;
                                    patchedKeyboards = true;
                                }
                            }
                        }
                    }
                }
            }

            if (!patchedSongs)     Console.WriteLine("ERROR: IsSongLocked not found — songs not patched.");
            if (!patchedKeyboards) Console.WriteLine("ERROR: GetActiveKeyboard not found — keyboards not patched.");

            if (patchedSongs || patchedKeyboards)
            {
                mod.Write(dll);
                if (patchedSongs)     Console.WriteLine("Songs patched successfully.");
                if (patchedKeyboards) Console.WriteLine("Keyboards patched successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRASH:");
            Console.WriteLine(ex.ToString());
        }

        Pause();
    }

    static FieldDef FindField(TypeDef type, string name)
    {
        foreach (var f in type.Fields)
            if (f.Name == name) return f;
        return null;
    }

    static void Pause()
    {
        Console.WriteLine();
        Console.Write("Press any key...");
        Console.ReadKey();
    }
}
