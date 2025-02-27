using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.Versioning;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Diagnostics.Eventing.Reader;
using Microsoft.Win32;

[SupportedOSPlatform("windows")]
class Program
{
    static readonly string[] NvidiaFiles = new[]
    {
        @"C:\ProgramData\NVIDIA Corporation\Drs\nvAppTimestamps",
        @"C:\ProgramData\NVIDIA Corporation\Drs\Driver.settings",
        @"C:\ProgramData\NVIDIA Corporation\Drs\GlobalPreferences.txt",
        @"C:\ProgramData\NVIDIA Corporation\Global\ProfileDB.bin",
        @"C:\ProgramData\NVIDIA Corporation\Global\Profiles.bin"
    };

    static void Main()
    {
        try
        {
            Console.Title = "Made by Amphetaminov";
            Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════╗");
            Console.WriteLine("║       NVIDIA Control Panel Cleaner           ║");
            Console.WriteLine("║     для очистки настроек любой программы     ║");
            Console.WriteLine("║          Made by Amphetaminov                ║");
            Console.WriteLine("╚══════════════════════════════════════════════╝");
            Console.WriteLine();
            
            if (!IsAdministrator())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Эта программа требует прав администратора!");
                Console.WriteLine("Пожалуйста, запустите программу от имени администратора.");
                WaitAndExit();
                return;
            }

            // Проверка активации
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Введите ключ активации:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            string? activationKey = Console.ReadLine()?.Trim().ToLower();

            if (activationKey != "amphetaminov")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Неверный ключ активации!");
                WaitAndExit();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Активация успешна!");
            Thread.Sleep(1000);
            ClearConsole();

            // Запрашиваем имя программы
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Введите имя программы (например: osu!.exe, dota2.exe, Steam.exe):");
            Console.ForegroundColor = ConsoleColor.Yellow;
            string? userInput = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(userInput))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Имя программы не может быть пустым!");
                WaitAndExit();
                return;
            }

            // Нормализуем ввод пользователя
            string searchPattern = userInput;
            if (!searchPattern.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                searchPattern += ".exe";
            }

            var foundFiles = new List<string>();
            var foundProgramInFiles = new List<(string File, List<string> FoundPatterns)>();
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Поиск файлов NVIDIA...");

            // Поиск всех файлов NVIDIA
            foreach (var file in NvidiaFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        foundFiles.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Предупреждение при поиске {file}: {ex.Message}");
                }
            }

            if (foundFiles.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Не найдено ни одного файла настроек NVIDIA!");
                WaitAndExit();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Найдено файлов настроек NVIDIA: {foundFiles.Count}");
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\nПоиск записей о {searchPattern}...");

            // Создаем варианты поиска (с учетом регистра и без .exe)
            var searchVariants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                searchPattern,
                searchPattern.ToLower(),
                searchPattern.ToUpper(),
                Path.GetFileNameWithoutExtension(searchPattern),
                Path.GetFileNameWithoutExtension(searchPattern).ToLower(),
                Path.GetFileNameWithoutExtension(searchPattern).ToUpper()
            };

            // Проверяем каждый файл
            foreach (var file in foundFiles)
            {
                try
                {
                    byte[] fileContent = File.ReadAllBytes(file);
                    var foundPatterns = new List<string>();
                    
                    foreach (var variant in searchVariants)
                    {
                        var patterns = new[] { 
                            Encoding.UTF8.GetBytes(variant),
                            Encoding.ASCII.GetBytes(variant),
                            Encoding.Unicode.GetBytes(variant)
                        };

                        foreach (var pattern in patterns)
                        {
                            if (FindBytes(fileContent, pattern) != -1)
                            {
                                foundPatterns.Add(variant);
                                break;
                            }
                        }
                    }

                    if (foundPatterns.Any())
                    {
                        foundProgramInFiles.Add((file, foundPatterns));
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Найдены записи в файле: {Path.GetFileName(file)}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"  Найденные варианты: {string.Join(", ", foundPatterns.Distinct())}");
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Ошибка при проверке файла {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            if (foundProgramInFiles.Any())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nНайдены записи о программе в {foundProgramInFiles.Count} файлах!");

                // Спрашиваем пользователя о продолжении
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\nВы уверены, что хотите удалить все найденные записи из NVIDIA Control Panel?");
                Console.WriteLine("Нажмите Y для подтверждения или любую другую клавишу для отмены...");
                Console.ResetColor();
                
                var key = Console.ReadKey(true);
                Console.WriteLine();
                
                if (key.Key == ConsoleKey.Y)
                {
                    ClearConsole();
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Удаление записей...");

                    foreach (var (file, patterns) in foundProgramInFiles)
                    {
                        try
                        {
                            byte[] fileContent = File.ReadAllBytes(file);
                            bool wasModified = false;

                            foreach (var variant in patterns)
                            {
                                var searchPatterns = new[] { 
                                    Encoding.UTF8.GetBytes(variant),
                                    Encoding.ASCII.GetBytes(variant),
                                    Encoding.Unicode.GetBytes(variant)
                                };

                                foreach (var pattern in searchPatterns)
                                {
                                    int pos;
                                    while ((pos = FindBytes(fileContent, pattern)) != -1)
                                    {
                                        // Заменяем найденную последовательность нулями
                                        for (int i = 0; i < pattern.Length; i++)
                                        {
                                            fileContent[pos + i] = 0;
                                        }
                                        wasModified = true;
                                    }
                                }
                            }

                            if (wasModified)
                            {
                                File.WriteAllBytes(file, fileContent);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Очищены записи в файле: {Path.GetFileName(file)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Ошибка при очистке файла {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nЗаписи о {searchPattern} успешно удалены из NVIDIA Control Panel!");
                    Console.WriteLine("Пожалуйста, перезапустите компьютер для применения изменений.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Операция отменена пользователем.");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Записи о {searchPattern} не найдены в файлах NVIDIA.");
                Console.WriteLine("Возможно, записи находятся в другом месте или в другом формате.");
            }
            
            WaitAndExit();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Произошла критическая ошибка: " + ex.Message);
            WaitAndExit();
        }
    }

    // Функция для поиска последовательности байт в массиве
    static int FindBytes(byte[] source, byte[] pattern)
    {
        for (int i = 0; i <= source.Length - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                return i;
            }
        }
        return -1;
    }
    
    static void ClearConsole()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine("║       NVIDIA Control Panel Cleaner           ║");
        Console.WriteLine("║     для очистки настроек любой программы     ║");
        Console.WriteLine("║          Made by Amphetaminov                ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    static void ShowCleanupProgress()
    {
        ClearConsole();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Зачистка следов началась...");
    }

    static void CleanupTraces()
    {
        try
        {
            ShowCleanupProgress();
            
            // Очистка Recent Files
            Thread.Sleep(300);
            string recentPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Recent"
            );

            var recentFiles = Directory.GetFiles(recentPath, "*.lnk")
                .Where(f => f.Contains("NvidiaCleaner") || f.Contains("Made by Amphetaminov"));

            foreach (var file in recentFiles)
            {
                try { File.Delete(file); } catch { }
            }

            // Очистка журнала событий
            Thread.Sleep(300);
            try
            {
                if (EventLog.Exists("Application"))
                {
                    EventLog log = new EventLog("Application");
                    log.Clear();
                    log.Close();
                }
            }
            catch { }

            // Очистка Prefetch
            Thread.Sleep(300);
            string prefetchPath = @"C:\Windows\Prefetch";
            var prefetchFiles = Directory.GetFiles(prefetchPath, "*NVIDIACLEANER*.pf");
            foreach (var file in prefetchFiles)
            {
                try { File.Delete(file); } catch { }
            }

            // Очистка временных файлов
            Thread.Sleep(300);
            string tempPath = Path.GetTempPath();
            var tempFiles = Directory.GetFiles(tempPath, "*NvidiaCleaner*");
            foreach (var file in tempFiles)
            {
                try { File.Delete(file); } catch { }
            }

            // Очистка Everything
            Thread.Sleep(300);
            string everythingDbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Everything"
            );
            try
            {
                if (Directory.Exists(everythingDbPath))
                {
                    foreach (var file in Directory.GetFiles(everythingDbPath, "*.db"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch { }

            // Очистка LastActivityView
            Thread.Sleep(300);
            string lastActivityPath = @"C:\Users\Public\AppData\Local\Microsoft\Windows\UsrClass.dat";
            if (File.Exists(lastActivityPath))
            {
                try { File.SetAttributes(lastActivityPath, FileAttributes.Normal); } catch { }
                try { File.Delete(lastActivityPath); } catch { }
            }

            // Очистка ShellBags
            Thread.Sleep(300);
            string[] shellBagPaths = {
                @"Software\Microsoft\Windows\Shell\Bags",
                @"Software\Microsoft\Windows\Shell\BagMRU",
                @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags",
                @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU"
            };

            foreach (var path in shellBagPaths)
            {
                try
                {
                    using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(path, true))
                    {
                        if (key != null)
                        {
                            foreach (string subKeyName in key.GetSubKeyNames())
                            {
                                try { key.DeleteSubKeyTree(subKeyName); } catch { }
                            }
                        }
                    }
                }
                catch { }
            }

            // Очистка реестра
            Thread.Sleep(300);
            string[] registryPaths = {
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU",
                @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched",
                @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRU"
            };

            foreach (var path in registryPaths)
            {
                try
                {
                    using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(path, true))
                    {
                        if (key != null)
                        {
                            foreach (string valueName in key.GetValueNames())
                            {
                                if (valueName.Contains("NvidiaCleaner", StringComparison.OrdinalIgnoreCase) ||
                                    valueName.Contains("Made by Amphetaminov", StringComparison.OrdinalIgnoreCase))
                                {
                                    try { key.DeleteValue(valueName); } catch { }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // Очистка USN журнала
            Thread.Sleep(300);
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "fsutil";
                    process.StartInfo.Arguments = "usn deletejournal /d c:";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch { }

            // Финальное сообщение
            ClearConsole();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Зачистка следов успешно закончена!");
            Thread.Sleep(1500);
        }
        catch { }
    }

    static void WaitAndExit()
    {
        Console.ResetColor();
        Console.WriteLine("\nНажмите любую клавишу для завершения работы...");
        Console.ReadKey(true);
        CleanupTraces();
        Thread.Sleep(500);
    }
    
    [SupportedOSPlatform("windows")]
    static bool IsAdministrator()
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
} 