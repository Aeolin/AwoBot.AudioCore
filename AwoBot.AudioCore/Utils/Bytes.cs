using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwoBot.AudioCore.Utils
{
  public static class Bytes
  {
    public sealed class ByteSizeFactor
    {
      public ByteSizeFactor(string name, string shortHand, long factor)
      {
        Name = name;
        ShortHand = shortHand;
        Factor = factor;
      }

      public string Name { get; init; }
      public string ShortHand { get; init; }
      public long Factor { get; init; }

      public long Convert(double value) => (long)(Factor * value);
    }

    public static ByteSizeFactor KiloFact { get; private set; } = new ByteSizeFactor("Kilobyte", "kb", 1024L);
    public static ByteSizeFactor MegaFact { get; private set; } = new ByteSizeFactor("Megabyte", "mb", 1024L * 1024);
    public static ByteSizeFactor GigaFact { get; private set; } = new ByteSizeFactor("Gigabyte", "gb", 1024L * 1024 * 1024);
    public static ByteSizeFactor TeraFact { get; private set; } = new ByteSizeFactor("Terabyte", "tb", 1024L * 1024 * 1024 * 1024);
    public static ByteSizeFactor PetaFact { get; private set; } = new ByteSizeFactor("Petabyte", "pb", 1024L * 1024 * 1024 * 1024 * 1024);
    public static ByteSizeFactor[] Factors { get; private set; } = new[] { KiloFact, MegaFact, GigaFact, TeraFact, PetaFact };

    public static long Kilo(double value) => KiloFact.Convert(value);
    public static long Mega(double value) => MegaFact.Convert(value);
    public static long Giga(double value) => GigaFact.Convert(value);
    public static long Tera(double value) => TeraFact.Convert(value); 
    public static long Peta(double value) => PetaFact.Convert(value);

    public static string Format(long value, string format) 
    {
      var factor = Factors.OrderBy(x => x.Factor).FirstOrDefault(x => (x.Factor / 2) > value, Factors.Last());
      return (((double)value) / factor.Factor).ToString(format);
    }
  }
}
