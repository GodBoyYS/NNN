using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class CodeZipper : EditorWindow
{
    [MenuItem("Tools/Code Zipper")]
    public static void ShowWindow()
    {
        GetWindow<CodeZipper>("Code Zipper");
    }

    private void OnGUI()
    {
        GUILayout.Space(20);
        GUILayout.Label("项目代码提取工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("该工具将提取 Assets/Scripts 下所有 .cs 文件，移除注释与换行，并打包成一个文本文件。\n\n文件将保存在：\n项目根目录/ProjectAllCodesList/", MessageType.Info);

        GUILayout.Space(20);

        if (GUILayout.Button("Start Zip (提取并压缩)", GUILayout.Height(40)))
        {
            ZipCodes();
        }
    }

    private void ZipCodes()
    {
        string scriptsFolder = Path.Combine(Application.dataPath, "Scripts");

        if (!Directory.Exists(scriptsFolder))
        {
            EditorUtility.DisplayDialog("错误", "未找到 'Assets/Scripts' 文件夹！\n请确认你的代码都在 Scripts 目录下。", "确定");
            return;
        }

        string[] scriptPaths = Directory.GetFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories);

        StringBuilder sb = new StringBuilder();
        int fileCount = 0;

        foreach (string path in scriptPaths)
        {
            // 排除 Editor 文件夹代码（建议开启）
            // if (path.Contains("Editor")) continue; 

            // [修改处] 使用 StreamReader 配合 Encoding.Default + detectEncodingFromByteOrderMarks
            // 这样既能读懂 UTF-8，也能读懂 GBK (中文默认)
            string content;
            using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default, true))
            {
                content = sr.ReadToEnd();
            }

            string compressedContent = CompressCode(content);

            string relativePath = path.Replace(Application.dataPath, "Assets");
            sb.Append($"\n[FILE START: {relativePath}]\n");
            sb.Append(compressedContent);
            sb.Append($"\n[FILE END]\n");

            fileCount++;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string targetDir = Path.Combine(projectRoot, "ProjectAllCodesList");

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        string outputFileName = GetNextVersionFileName(targetDir);
        string finalPath = Path.Combine(targetDir, outputFileName);

        // 写入时保持 UTF-8 是对的，这是通用的输出格式
        File.WriteAllText(finalPath, sb.ToString(), Encoding.UTF8);

        EditorUtility.DisplayDialog("完成",
            $"成功提取 {fileCount} 个脚本文件。\n(来源: Assets/Scripts)\n\n已保存至:\n{finalPath}",
            "确定");

        EditorUtility.RevealInFinder(finalPath);
    }

    private string CompressCode(string source)
    {
        // 移除块注释 /* ... */
        string noBlockComments = Regex.Replace(source, @"/\*[\s\S]*?\*/", "");
        // 移除行注释 // ...
        string noLineComments = Regex.Replace(noBlockComments, @"//.*", "");
        // 将所有空白字符（换行、制表符、多余空格）替换为单个空格
        string singleLine = Regex.Replace(noLineComments, @"\s+", " ");
        return singleLine.Trim();
    }

    private string GetNextVersionFileName(string folderPath)
    {
        string baseName = "ProjectAllCodesV";
        string extension = ".txt";
        int version = 1;

        while (true)
        {
            string fileName = $"{baseName}{version}{extension}";
            string fullPath = Path.Combine(folderPath, fileName);
            if (!File.Exists(fullPath))
            {
                return fileName;
            }
            version++;
        }
    }
}
