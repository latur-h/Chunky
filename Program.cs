using Chunky.Logic;

namespace Chunky
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length == 0) { ConsoleEx.Error("Invalid argument!"); return; }
            else
            {
                if (args.Contains("-t") && args.Contains("-f")) { ConsoleEx.Error("Invalid argument! You cannot use both functions at the same time."); return; }
                else if (args.Contains("-t"))
                {
                    if (args.Length != 4) { ConsoleEx.Error("Invalid parameters."); return; }

                    string sourceFile = string.Empty; string destinationFolder = string.Empty; long blockSize;

                    if (!isExist(args[1], true, out sourceFile)) { ConsoleEx.Error("Source file doesn't exist!"); return; }
                    if (!isExist(args[2], false, out destinationFolder))
                    {
                        try
                        {
                            Directory.CreateDirectory(args[2]);

                            destinationFolder = Path.GetFullPath(args[2]);

                            ConsoleEx.Info($"Directory created with next name: {destinationFolder}");
                        }
                        catch (Exception ex)
                        {
                            ConsoleEx.Error($"Folder creation is failed with next message:\n{ex.Message}\nMaking a new one...");

                            string? _dirName = Path.GetDirectoryName(Path.GetFullPath(sourceFile));

                            if (string.IsNullOrEmpty(_dirName))
                            {
                                ConsoleEx.Error("Cannot specify the parent folder.");
                                return;
                            }

                            do
                            {
                                destinationFolder = Path.Combine(_dirName, Guid.NewGuid().ToString());
                            } while (Directory.Exists(destinationFolder));

                            Directory.CreateDirectory(destinationFolder);

                            ConsoleEx.Info($"Directory created with next name: {destinationFolder}");
                        }
                    }

                    try
                    {
                        blockSize = Convert.ToInt64(string.Join("", args[3].Where(Char.IsDigit)));
                    }
                    catch { ConsoleEx.Error("Cannot parse size of blocks."); return; }

                    switch (string.Join("", args[3].Where(Char.IsLetter)).ToLower())
                    {
                        case "bytes":
                            break;
                        case "kb":
                            blockSize *= (long)Math.Pow(2, 10);
                            break;
                        case "mb":
                            blockSize *= (long)Math.Pow(2, 20);
                            break;
                        case "gb":
                            blockSize *= (long)Math.Pow(2, 30);
                            break;
                        default:
                            ConsoleEx.Error("Cannot parse size of blocks.");
                            return; 
                    }

                    TF tf = new();

                    var result = tf.To_ManyFiles(sourceFile, destinationFolder, blockSize);

                    if (result.status)
                        ConsoleEx.Info($"Created {result.amount} files.");
                    else
                    {
                        ConsoleEx.Error($"{result.reason}");
                    }
                }
                else if (args.Contains("-f"))
                {
                    if (args.Length != 3) { ConsoleEx.Error("Invalid parameters."); return; }

                    string sourceFolder = string.Empty; string destinationFile = string.Empty;

                    if (!isExist(args[1], false, out sourceFolder)) { ConsoleEx.Error("Path to directory doesn't exist!"); return; }
                    if (isExist(args[2], true, out destinationFile)) { ConsoleEx.Error("A file with the same name already exists! Change this name or delete the file."); return; }

                    TF tf = new();

                    try { tf.From_ManyFiles(sourceFolder, Path.GetFullPath(args[2])); ConsoleEx.Info("Succeed!"); }
                    catch (Exception ex) { ConsoleEx.Error(ex.Message); return; }
                }
                else ConsoleEx.Error("Invalid arguments.");
            }

            bool isExist(string path, bool isFile, out string fullPath)
            {
                path = Path.GetFullPath(path);
                if (isFile)
                {
                    if (Path.Exists(path) && File.Exists(path))
                    {
                        fullPath = path;
                        return true;
                    }
                    else
                    {
                        fullPath = string.Empty;
                        return false;
                    }
                }
                else
                {
                    if (Path.Exists(path) && Directory.Exists(path))
                    {
                        fullPath = path;
                        return true;
                    }
                    else
                    {
                        fullPath = string.Empty;
                        return false;
                    }
                }
            }
        }
    }
}