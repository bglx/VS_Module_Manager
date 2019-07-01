﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using module_manager;

public class Functions
{
    
    public static bool affiche = false;
    public static List<string> descList = new List<string>();
    Config config = new Config();

    public List<string> SearchGitmodulesFile(string path)
    {
        List<string> repo = new List<string>();
        try
        {
            foreach (string f in Directory.GetFiles(path))
            {
                if (f.Contains(".gitmodules"))
                {
                    int counter = 0;
                    string line;
                    StreamReader file = new StreamReader(f);
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line.Contains("url"))
                        {
                            repo.Add(line.Replace("url = ", ""));
                        }
                        counter++;
                    }
                    file.Close();
                }
            }
        }
        catch (Exception excpt)
        {
            Console.WriteLine(excpt.Message);
        }

        return repo;
    }

    public List<string> DispRepoList()
    {
        List<string> repoList = new List<string>();
        if (config.GetCurrentType() == "gitblit")
        {
            string json;
            using (var client = new WebClient())
            {
                json = client.DownloadString(config.GetServerUrl() + "rpc/?req=LIST_REPOSITORIES");
            }

            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);

            JObject list = JObject.Parse(json);
            var properties = list.Properties();
            foreach (var prop in properties)
            {
                JObject infos = JObject.FromObject(list[prop.Name]);
                if (!((string)infos["name"]).Contains("~"))
                {
                    repoList.Add(infos["name"].ToString());
                    try
                    {
                        string desc = infos["description"].ToString();
                        descList.Add(desc);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

            }
        }
        return repoList;
    }

    public List<string> DispModList()
    {
        List<string> repoList = new List<string>();
        if (config.GetCurrentType() == "gitblit")
        {
            string json;
            using (var client = new WebClient())
            {
                json = client.DownloadString(config.GetServerUrl() + "rpc/?req=LIST_REPOSITORIES");
            }

            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);

            JObject list = JObject.Parse(json);
            var properties = list.Properties();
            foreach (var prop in properties)
            {
                JObject infos = JObject.FromObject(list[prop.Name]);
                if (!((string)infos["name"]).Contains("~") && ((string)infos["name"]).Contains("MODULES"))
                {
                    repoList.Add(infos["name"].ToString());
                    try
                    {
                        string desc = infos["description"].ToString();
                        descList.Add(desc);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

            }
        }
        return repoList;
    }

    public string DispServerList()
    {
        List<string> servList = new List<string>();
        string json;
        json = File.ReadAllText(@"C:\Users\STBE\source\repos\module_manager\module_manager\bin\Debug\servers.json");

        byte[] bytes = Encoding.Default.GetBytes(json);
        json = Encoding.UTF8.GetString(bytes);

        
        return json;
    }

    public List<string> GetModuleDep(string moduleText, string branch)
    {
        List<string> dep = new List<string>();
        if(config.GetCurrentType() == "gitblit")
        {
            string url = config.GetServerUrl() + "raw/" + moduleText + "/" + branch + "/.dependencies";
            var client = new WebClient();
            using (var stream = client.OpenRead(url))
            using (var reader = new StreamReader(stream))
            {
                string ligne;
                while ((ligne = reader.ReadLine()) != null)
                {
                    if (ligne.Contains("branch") && branch != "master")
                    {
                        dep.AddRange(GetModuleDep(moduleText, "master"));
                        break;
                    }
                    if (!ligne.Contains("Error"))
                    {
                        dep.Add(ligne);
                    }
                }
            }
        }

        return dep;
    }

    public static bool ShowDialog(string text, string caption)
    {
        Form prompt = new Form();
        prompt.AutoSize = true;
        prompt.AutoSizeMode = AutoSizeMode.GrowOnly;
        prompt.Padding = new Padding(20, 20, 20, 0);
        prompt.Size = new System.Drawing.Size(300, 200);
        FlowLayoutPanel panel = new FlowLayoutPanel();
        panel.Dock = DockStyle.Fill;
        CheckBox chk = new CheckBox();
        chk.Text = text;
        Label label = new Label();
        label.Text = caption;
        label.AutoSize = true;
        Button ok = new Button() { Text = "OK" };
        ok.Click += (sender, e) => { prompt.Close(); };
        panel.Controls.Add(label);
        panel.Controls.Add(chk);
        panel.SetFlowBreak(chk, true);
        panel.Controls.Add(ok);
        prompt.Controls.Add(panel);
        prompt.ShowDialog();
        return chk.Checked;
    }

    public List<string> GetModList(string branch, string rep)
    {
        List<string> modList = new List<string>();
        if(config.GetCurrentType() == "gitblit")
        {
            string url = config.GetServerUrl() + "raw/" + rep + "/" + branch + "/.gitmodules";
            Console.WriteLine(url);
            var client = new WebClient();
            using (var stream = client.OpenRead(url))
            using (var reader = new StreamReader(stream))
            {
                string ligne;
                string ligne1 = "", ligne2 = "", ligne3 = "";
                int compteur = 1;
                while ((ligne = reader.ReadLine()) != null)
                {
                    if (ligne.Contains("branch") && branch != "master")
                    {
                        if (affiche)
                        {
                            affiche = !ShowDialog("Ne plus afficher", "Le projet " + rep + " ne contient pas de branche _DEV_ ! \n\nRecherche sur la branche master\n ");
                        }
                        modList.AddRange(GetModList("master", rep));
                        break;
                    }
                    switch (compteur)
                    {
                        case 1:
                            ligne1 = ligne;
                            break;
                        case 2:
                            ligne2 = ligne;
                            break;
                        case 3:
                            ligne3 = ligne;
                            compteur = 1;
                            break;
                    }
                    if (ligne.Contains("url"))
                    {
                        modList.Add(ligne.Replace("url = ", ""));
                    }
                }
            }
        }
        return modList;
    }

    public string GetProjFullName(string path)
    {
        string line = "";
        string prev = "";
        string fullName = "";
        StreamReader file = new StreamReader(path + @"\.git\config");
        while ((line = file.ReadLine()) != null)
        {
            if (line.Contains("remote"))
            {
                prev = line;
            }
            if(prev.Contains("remote") && line.Contains("url"))
            {
                fullName = line.Replace("url = ","");
                break;
            }
        }
        file.Close();
        fullName = fullName.Substring(fullName.IndexOf(@"/r/") + 3, fullName.Length - fullName.IndexOf(@"/r/") - 3);
        Console.WriteLine(fullName);
        return fullName;

    }

    public List<string> GetCheckedNodes(TreeNodeCollection treeNode)
    {
        List<string> checkedList = new List<string>();

        foreach (TreeNode node in treeNode)
        {
            if (node.Checked)
            {
                checkedList.Add(node.Text);
            }

            if(node.Nodes.Count != 0)
            {
                checkedList.AddRange(GetCheckedNodes(node.Nodes));
            }
        }

        return checkedList;
    }

    public string GetMarkdown(string projName, string branch)
    {
        string md = "";
        if (config.GetCurrentType() == "gitblit")
        {
            try
            {
                using (var wc = new System.Net.WebClient())
                    md = wc.DownloadString(config.GetServerUrl() + @"raw/" + projName + @".git/" + branch + @"/README.md");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed downloading README.md : " + ex.Message);
            }
            if (md.Contains("branch") && branch != "master")
            {
                return GetMarkdown(projName, "master");
            }
            byte[] bytes = Encoding.Default.GetBytes(md);
            md = Encoding.UTF8.GetString(bytes);
        }
        return md;
    }

    public string GetRepoListBitBucket()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("");

        byte[] entropy = new byte[20]; //TODO: Change to global password
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(entropy);
        }

        byte[] ciphertext = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);

        File.WriteAllBytes(@"./.creditEnt", entropy);
        File.WriteAllBytes(@"./.creditCip", ciphertext);
        
        return "OK";
    }

}
