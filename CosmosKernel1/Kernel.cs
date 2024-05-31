using Cosmos.HAL.BlockDevice;
using System;
using System.Collections.Generic;
using System.IO;
using Sys = Cosmos.System;

namespace CosmosKernel1
{
    public class Kernel : Sys.Kernel
    {
        private MemoryManager memoryManager;
        private Sys.FileSystem.CosmosVFS fs;
        private string current_directory = "0:\\";
        private List<Process> processList = new List<Process>();
        private readonly object processListLock = new object();

        protected override void BeforeRun()
        {
            memoryManager = new MemoryManager(10000);
            memoryManager.InitializeMemoryBlocks();

            Console.WriteLine("Booting on os.");
            Console.WriteLine(".....");
            Console.WriteLine(".....");
            Console.WriteLine("Booting done.");
            Console.WriteLine("Welcome to the BASIC Real Time Operating System.");
            Console.WriteLine("Type 'help' to see all commands.");
            fs = new Sys.FileSystem.CosmosVFS();
            Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);

            StartAlwaysRunningProcesses();
        }

        protected override void Run()
        {
            Console.Write(">");
            string input = Console.ReadLine();
            ExecuteCommand(input);
        }

        private void DisplayHelp()
        {
            Console.WriteLine("\n1 - Memory Management\n2 - Process\n3 - VFS\n4 - Memory Info\n5 - Shutdown\n");
        }

        private void MemoryManagementMenu()
        {
            Console.WriteLine("\nMemory Management:\nallocate - Allocate memory\nfree - Free memory\n");

            Console.Write(">");
            string input = Console.ReadLine();

            if (input == "allocate")
            {
                AllocateMemory();
            }
            else if (input == "free")
            {
                FreeMemory();
            }
            else
            {
                Console.WriteLine("Invalid command in Memory Management menu!");
            }
        }

        private void ProcessMenu()
        {
            Console.WriteLine("\nProcess:\nlist - List processes\nschedule - Schedule a process\n");

            Console.Write(">");
            string input = Console.ReadLine();

            if (input == "list")
            {
                ListProcesses();
            }
            else if (input == "schedule")
            {
                Console.WriteLine("Enter the process name:");
                string processName = Console.ReadLine();
                StartProcess(processName);
            }
            else
            {
                Console.WriteLine("Invalid command in Process menu!");
            }
        }

        private void VFSMenu()
        {
            Console.WriteLine("\nVFS:\nlistfile/ls - List files\ndeletefile/rm - Delete file\nwritefile - Write to file\nreadfile/cat - Read file\ncreatedirectory/mkdir - Create directory\nchange_directory/cd - Change directory\npwd - Print working directory\ncreatefile/touch - Create file\nexit - Exit to main menu\n");

            while (true)
            {
                Console.Write(">");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "listfile":
                    case "ls":
                        ListFiles();
                        break;
                    case "deletefile":
                    case "rm":
                        DeleteFile();
                        break;
                    case "writefile":
                        WriteFile();
                        break;
                    case "readfile":
                    case "cat":
                        ReadFile();
                        break;
                    case "createdirectory":
                    case "mkdir":
                        CreateDirectory();
                        break;
                    case "change_directory":
                    case "cd":
                        ChangeDirectory();
                        break;
                    case "pwd":
                        Console.WriteLine($"Current directory: {current_directory}\n");
                        break;
                    case "createfile":
                    case "touch":
                        CreateFile();
                        break;
                    case "exit":
                        return;
                    default:
                        Console.WriteLine("Invalid command in VFS menu!");
                        break;
                }
            }
        }

        private void AllocateMemory()
        {
            try
            {
                int address = memoryManager.AllocateMemory();
                Console.WriteLine("Memory allocated at address: " + address);
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void FreeMemory()
        {
            Console.Write("Enter address to free: ");
            int address = int.Parse(Console.ReadLine());

            try
            {
                memoryManager.FreeMemory(address);
                Console.WriteLine("Memory freed at address: " + address);
                RemoveProcessByMemoryAddress(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ListFiles()
        {
            try
            {
                string[] files = Directory.GetFiles(current_directory);
                string[] directories = Directory.GetDirectories(current_directory);

                Console.WriteLine("Files in the current directory:");
                foreach (string file in files)
                {
                    Console.WriteLine(Path.GetFileName(file));
                }

                Console.WriteLine("\nDirectories in the current directory:");
                foreach (string directory in directories)
                {
                    Console.WriteLine(Path.GetFileName(directory));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing files: {ex.Message}\n");
            }
        }

        private void DeleteFile()
        {
            Console.WriteLine("Enter the file name (including extension) to delete:");
            string fileName = Console.ReadLine();

            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    throw new ArgumentException("Please provide a valid filename.\n");
                }

                string filePath = Path.Combine(current_directory, fileName);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"{fileName} does not exist.\n");
                    return;
                }

                File.Delete(filePath);
                Console.WriteLine($"{fileName} deleted successfully!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message} \n");
            }
        }

        private void WriteFile()
        {
            Console.WriteLine("Enter the file name (including extension):");
            string fileName = Console.ReadLine();

            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    throw new ArgumentException("Please provide a valid filename.\n");
                }

                string filePath = Path.Combine(current_directory, fileName);

                Console.WriteLine("Enter the text to write to the file:");
                string fileContent = Console.ReadLine();

                File.WriteAllText(filePath, fileContent);
                Console.WriteLine($"Content written to {fileName} successfully!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to file: {ex.Message} \n");
            }
        }

        private void ReadFile()
        {
            Console.WriteLine("Enter the file name (including extension) to display:");
            string fileName = Console.ReadLine();

            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    throw new ArgumentException("Please provide a valid filename.\n");
                }

                string filePath = Path.Combine(current_directory, fileName);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"{fileName} does not exist.\n");
                    return;
                }

                string fileContent = File.ReadAllText(filePath);
                Console.WriteLine(fileContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying file content: {ex.Message} \n");
            }
        }

        private void CreateDirectory()
        {
            Console.WriteLine("Enter the directory name:");
            string directoryName = Console.ReadLine();

            try
            {
                if (string.IsNullOrEmpty(directoryName))
                {
                    throw new ArgumentException("Please provide a valid directory name.\n");
                }

                string directoryPath = Path.Combine(current_directory, directoryName);

                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"Directory '{directoryName}' created successfully!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating directory: {ex.Message}\n");
            }
        }

        private void ChangeDirectory()
        {
            Console.WriteLine("Enter the directory name:");
            string directoryName = Console.ReadLine();

            try
            {
                string newDirectoryPath = Path.Combine(current_directory, directoryName);

                if (!Directory.Exists(newDirectoryPath))
                {
                    Console.WriteLine($"{directoryName} does not exist.\n");
                    return;
                }

                current_directory = newDirectoryPath;
                Console.WriteLine($"Current directory changed to {current_directory}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing directory: {ex.Message}\n");
            }
        }

        private void CreateFile()
        {
            Console.WriteLine("Enter the file name (including extension):");
            string fileName = Console.ReadLine();

            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    throw new ArgumentException("Please provide a valid filename.\n");
                }

                string filePath = Path.Combine(current_directory, fileName);

                using (FileStream fs = File.Create(filePath))
                {
                    Console.WriteLine($"{fileName} created successfully!\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating file: {ex.Message}\n");
            }
        }

        private void DisplayMemoryInfo()
        {
            try
            {
                uint availableMemory = Cosmos.Core.CPU.GetAmountOfRAM() * 1024 * 1024;
                uint usedMemory = Cosmos.Core.GCImplementation.GetUsedRAM();
                Console.WriteLine($"Available Memory: {availableMemory} bytes\nUsed Memory: {usedMemory} bytes\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying memory information: {ex.Message}\n");
            }
        }

        private void Shutdown()
        {
            Console.WriteLine("Shutting down...");
            Cosmos.System.Power.Shutdown();
        }

        private void StartAlwaysRunningProcesses()
        {
            
        }

        private void StartProcess(string processName)
        {
            try
            {
                int memoryAddress = memoryManager.AllocateMemory();
                var process = new Process(processName, memoryAddress);
                lock (processListLock)
                {
                    processList.Add(process);
                }
                Console.WriteLine($"Process '{processName}' started at memory address {memoryAddress}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start process '{processName}': {ex.Message}");
            }
        }

        private void ListProcesses()
        {
            lock (processListLock)
            {
                Console.WriteLine("Running Processes:");
                foreach (var process in processList)
                {
                    Console.WriteLine($"Process Name: {process.Name}, Memory Address: {process.MemoryAddress}");
                }
            }
        }

        private void RemoveProcessByMemoryAddress(int memoryAddress)
        {
            lock (processListLock)
            {
                processList.RemoveAll(p => p.MemoryAddress == memoryAddress);
            }
        }

        private void ExecuteCommand(string command)
        {
            switch (command.ToLower())
            {
                case "help":
                    DisplayHelp();
                    break;
                case "1":
                    MemoryManagementMenu();
                    break;
                case "2":
                    ProcessMenu();
                    break;
                case "3":
                    VFSMenu();
                    break;
                case "4":
                    DisplayMemoryInfo();
                    break;
                case "5":
                    Shutdown();
                    break;
                default:
                    Console.WriteLine("Unknown command! Type 'help' to see all commands.");
                    break;
            }
        }
    }

    public class Process
    {
        public string Name { get; }
        public int MemoryAddress { get; }

        public Process(string name, int memoryAddress)
        {
            Name = name;
            MemoryAddress = memoryAddress;
        }
    }

    public class MemoryManager
    {
        private const int BlockSize = 4096;
        private readonly List<bool> memoryMap;

        public MemoryManager(int totalBlocks)
        {
            memoryMap = new List<bool>(totalBlocks);
            for (int i = 0; i < totalBlocks; i++)
            {
                memoryMap.Add(false);
            }
        }

        public void InitializeMemoryBlocks()
        {
            for (int i = 0; i < memoryMap.Count; i++)
            {
                memoryMap[i] = false;
            }
        }

        public int AllocateMemory()
        {
            for (int i = 0; i < memoryMap.Count; i++)
            {
                if (!memoryMap[i])
                {
                    Console.WriteLine($"Allocating memory block {i}");
                    memoryMap[i] = true;
                    return i * BlockSize;
                }
            }
            throw new OutOfMemoryException("Insufficient memory");
        }

        public void FreeMemory(int address)
        {
            int index = address / BlockSize;

            if (index < 0 || index >= memoryMap.Count || !memoryMap[index])
            {
                Console.WriteLine($"Invalid memory address: {address}");
                throw new ArgumentException("Invalid memory address");
            }

            Console.WriteLine($"Freeing memory block {index}");
            memoryMap[index] = false;
        }
    }
}
