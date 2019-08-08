using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using ModManagerNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModManagerInstall
{
    public static class Log
    {

        public static void Print(string log)
        {
            MessageBox.Show(log);
        }
    }

    public partial class Form1 : Form
    {
        static string currentManagedPath = "";
        static string currentGamePath = "";

        private enum Actions
        {
            Install,
            Remove
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Install_Click(object sender, EventArgs e)
        {
            Inject(Actions.Install);
        }

        private void Unstall_Click(object sender, EventArgs e)
        {
            Inject(Actions.Remove);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Button1_Click(object sender, EventArgs e)
        {

        }

        private bool Inject(Actions action, ModuleDefMD assembly = null, bool save = true)
        {
            currentGamePath = Path.GetDirectoryName(PathOfHacknet.Text);
            currentManagedPath = System.Environment.CurrentDirectory;
            string PatchTarget = "Hacknet.Program.Main:Before";
            string text = PathOfHacknet.Text;
            string destFileName = $"{text}.backup";
            if (File.Exists(text))
            {
                if (assembly == null)
                    try
                    {
                            assembly = ModuleDefMD.Load(File.ReadAllBytes(text));
                    }
                    catch (Exception ex)
                    {
                        Log.Print(ex.Message);
                        return false;
                    }
                string className = null;
                string methodName = null;
                string text3 = null;
                int num = PatchTarget.LastIndexOf('.');
                if (num != -1)
                {
                    className = PatchTarget.Substring(0, num);
                    int num2 = PatchTarget.LastIndexOf(':');
                    if (num2 != -1)
                    {
                        methodName = PatchTarget.Substring(num + 1, num2 - num - 1);
                        text3 = PatchTarget.Substring(num2 + 1).ToLower();
                        if (text3 != "after" && text3 != "before")
                            Log.Print($"Parameter '{text3}' in '{PatchTarget}' is unknown.");
                    }
                    else
                        methodName = PatchTarget.Substring(num + 1);

                    if (methodName == "ctor")
                        methodName = ".ctor";

                    TypeDef typeDef = assembly.Types.FirstOrDefault((TypeDef x) => x.FullName == className);
                    if (typeDef == null)
                    {
                        Log.Print($"Class '{className}' not found.");
                        return false;
                    }

                    MethodDef methodDef = typeDef.Methods.FirstOrDefault((MethodDef x) => x.Name == methodName);
                    if (methodDef == null)
                    {
                        Log.Print($"Method '{methodName}' not found.");
                        return false;
                    }

                    Type modManagerType = typeof(ModManager);
                    switch (action)
                    {
                        case Actions.Install:
                            try
                            {
                                Log.Print($"Backup for Hacknet.exe.");
                                File.Copy(text, destFileName, overwrite: true);
                                CopyLibraries();

                                string path = Path.Combine(currentGamePath, "Mods");
                                if (!Directory.Exists(path))
                                    Directory.CreateDirectory(path);

                                if (assembly.Types.FirstOrDefault((TypeDef x) => x.Name == modManagerType.Name) != null && !Inject(Actions.Remove, assembly, save: false))
                                {
                                    Log.Print("Installation failed. Can't uninstall the previous version.");
                                    return false;
                                }

                                Log.Print("Applying patch...");
                                ModuleDefMD moduleDefMD = ModuleDefMD.Load(modManagerType.Module);
                                var t = moduleDefMD.Types;

                                Log.Print("Applying patch...");
                                TypeDef typeDef3 = t.First((TypeDef x) => x.Name == modManagerType.Name );

                                Log.Print($"moduleDefMD.Types.Remove(typeDef3).");
                                moduleDefMD.Types.Remove(typeDef3);
                                assembly.Types.Add(typeDef3);

                                Log.Print($"Call Start");
                                Instruction item = OpCodes.Call.ToInstruction(typeDef3.Methods.First((MethodDef x) => x.Name == "Start"));

                                Log.Print($"Insert");
                                if (string.IsNullOrEmpty(text3) || text3 == "after")
                                    methodDef.Body.Instructions.Insert(methodDef.Body.Instructions.Count - 1, item);
                                else if (text3 == "before")
                                {
                                    methodDef.Body.Instructions.Insert(0, item);
                                }

                                Log.Print($"Save");
                                if (save)
                                {
                                    assembly.Write(text);
                                    Log.Print("安装成功.");
                                }
                                Install.Enabled = false;
                                Unstall.Enabled = true;
                                return true;
                            }
                            catch (Exception ex3)
                            {
                                Log.Print(ex3.Message);
                                if (!File.Exists(text))
                                    RestoreBackup();
                            }
                            break;
                        case Actions.Remove:
                            try
                            {
                                TypeDef typeDef2 = assembly.Types.FirstOrDefault((TypeDef x) => x.Name == modManagerType.Name);
                                if (typeDef2 != null)
                                {
                                    Log.Print("移除ModManager...");
                                    Instruction instruction = OpCodes.Call.ToInstruction(typeDef2.Methods.First((MethodDef x) => x.Name == "Start"));
                                    for (int i = 0; i < methodDef.Body.Instructions.Count; i++)
                                    {
                                        if (methodDef.Body.Instructions[i].OpCode == instruction.OpCode && methodDef.Body.Instructions[i].Operand == instruction.Operand)
                                        {
                                            methodDef.Body.Instructions.RemoveAt(i);
                                            break;
                                        }
                                    }

                                    assembly.Types.Remove(typeDef2);
                                    if (save)
                                    {
                                        assembly.Write(text);
                                        Log.Print("移除成功.");
                                    }
                                    Install.Enabled = true;
                                    Unstall.Enabled = false;
                                }
                                return true;
                            }
                            catch (Exception ex2)
                            {
                                Log.Print(ex2.Message);
                                if (!File.Exists(text))
                                    RestoreBackup();
                            }
                            break;
                    }
                    return false;
                }
                Log.Print($"Function name error '{PatchTarget}'.");
                return false;
            }
            Log.Print($"'{text}' 无法找到.");
            return false;
        }

        private static void CopyLibraries()
        {
            string[] array = new string[] { "0Harmony12.dll" , "Newtonsoft.Json.dll" };
            foreach (string text in array)
            {
                string text2 = Path.Combine(currentGamePath, text);
                if (File.Exists(text2))
                {
                    FileInfo fileInfo = new FileInfo(Path.Combine(currentManagedPath, text));
                    if (new FileInfo(text2).Length == fileInfo.Length)
                        continue;
                    File.Copy(text2, $"{text2}.backup", overwrite: true);
                }
                File.Copy(Path.Combine(currentManagedPath, text), text2, overwrite: true);
                Log.Print($"'{text}' 已经复制到游戏目录.");
            }
        }

        private static bool RestoreBackup()
        {
            string text = Path.Combine(currentGamePath, "Hacknet.exe");
            string text2 = $"{text}.backup";
            try
            {
                if (File.Exists(text2))
                {
                    File.Copy(text2, text, overwrite: true);
                    Log.Print("备份已恢复.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
            return false;
        }

    }
}
