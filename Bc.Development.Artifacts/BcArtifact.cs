﻿using System;
using System.IO;
using System.Threading.Tasks;
using Bc.Development.Configuration;

namespace Bc.Development.Artifacts
{

  public class BcArtifact
  {

    public static async Task<BcArtifact> FromLocalFolder(string folder, ArtifactStorageAccount? account = null)
    {
      var config = await BcContainerHelperConfiguration.Load();
      var relativePart = folder.Substring(config.BcArtifactsCacheFolder.Length + 1);
      var parts = relativePart.Split('\\', '/');
      return new BcArtifact
      {
        StorageAccount = account,
        Type = (ArtifactType)Enum.Parse(typeof(ArtifactType), parts[0], true),
        Version = new Version(parts[1]),
        Country = parts[2],
        Uri = null
      };
    }

    public static BcArtifact FromUri(Uri artifactUri)
    {
      var parts = $"{artifactUri}".Split('/');
      var accountType = parts[2].Split('.')[0];

      return new BcArtifact
      {
        StorageAccount = (ArtifactStorageAccount?)(String.IsNullOrEmpty(accountType) ? null : Enum.Parse(typeof(ArtifactStorageAccount), accountType, true)),
        Type = (ArtifactType)Enum.Parse(typeof(ArtifactType), parts[3], true),
        Version = new Version(parts[4]),
        Country = parts[5],
        Uri = artifactUri
      };
    }


    public bool IsPlatform => Country.Equals(Defaults.PlatformIdentifier, StringComparison.OrdinalIgnoreCase);

    public Version Version { get; private set; }

    public string Country { get; private set; }

    public Uri Uri { get; private set; }

    public ArtifactStorageAccount? StorageAccount { get; private set; }

    public ArtifactType Type { get; private set; }


    private BcArtifact()
    {
    }


    public async Task<DateTime?> GetLastUsedDate()
    {
      var localFolder = await GetLocalFolder();
      if (!localFolder.Exists) return null;
      var fi = new FileInfo(Path.Combine(localFolder.FullName, "lastused"));
      if (!fi.Exists) return null;
      using (var sr = fi.OpenText())
        return new DateTime(long.Parse(await sr.ReadLineAsync()));
    }

    public async Task<bool> SetLastUsedDate(DateTime? tag = null)
    {
      var localFolder = await GetLocalFolder();
      if (!localFolder.Exists) return false;
      try
      {
        var dateTime = (tag ?? DateTime.Now).ToUniversalTime();
        using (var s = File.CreateText(Path.Combine(localFolder.FullName, "lastused")))
          await s.WriteLineAsync($"{dateTime.Ticks}");
        return true;
      }
      catch
      {
        return false;
      }
    }

    public BcArtifact CreatePlatformArtifact()
    {
      if (IsPlatform) return this;
      var uri = ArtifactReader.MakeArtifactUri(
        StorageAccount ?? Defaults.DefaultStorageAccount,
        Type,
        Version,
        Defaults.PlatformIdentifier);
      return FromUri(uri);
    }

    public async Task<DirectoryInfo> GetLocalFolder()
    {
      var config = await BcContainerHelperConfiguration.Load();
      var folder = Path.Combine(
        config.BcArtifactsCacheFolder,
        Type.ToString().ToLowerInvariant(),
        Version.ToString(),
        Country);
      return new DirectoryInfo(folder);
    }


    public override string ToString()
    {
      return $"{Type} {Version} ({Country})";
    }

  }

}