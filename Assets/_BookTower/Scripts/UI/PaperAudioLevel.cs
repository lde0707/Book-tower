using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace BookTower.UI
{
 public enum PaperAudioBus{Effects,Music,Ambience}
 public sealed class PaperAudioLevel:MonoBehaviour
 {
  public PaperAudioBus bus;public float baseline=1;AudioSource source;
  public static void Bind(AudioSource source,PaperAudioBus bus=PaperAudioBus.Effects){var level=source.GetComponent<PaperAudioLevel>();if(!level)level=source.gameObject.AddComponent<PaperAudioLevel>();level.source=source;level.baseline=source.volume;level.bus=bus;level.LateUpdate();}
  void LateUpdate(){if(!source)return;var s=PaperSettingsRuntime.Values;source.volume=baseline*(bus==PaperAudioBus.Music?s.music:bus==PaperAudioBus.Ambience?s.ambience:s.effects);}
 }
}
