using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// A seeded generator of JSON document pairs for the equivalence guard: an expected document and an actual one derived
/// from it by one to three mutations (another number form, another property order, a changed, added or removed value, a
/// changed kind, a reordered, shortened or longer array). The generator is its own SplitMix64, so a seed produces the
/// same pairs on every .NET version. Numbers stay within <see cref="decimal"/> precision, so the comparison of a
/// number pair does not depend on how literals beyond it are compared.
/// </summary>
internal static class RandomJsonPairs
{
    private static readonly string[] Names = ["a", "b", "c", "d", "e"];
    private static readonly string[] Strings = ["x", "y", "z", ""];

    // Each row: forms of one value. A form change keeps the value; a value change picks another row.
    private static readonly string[][] Numbers =
    [
        ["0", "-0", "0.0", "0e5"],
        ["1", "1.0", "1e0", "10e-1", "1E+0"],
        ["-1", "-1.0", "-1e0"],
        ["2", "2.00", "0.2e1"],
        ["10", "1e1", "10.0"],
        ["100", "1e2", "1E2", "100.0"],
        ["1.5", "15e-1", "1.50"],
        ["-0.25", "-25e-2", "-0.250"],
    ];

    /// <summary>Generates <paramref name="count"/> pairs from <paramref name="seed"/>.</summary>
    /// <param name="seed">The seed.</param>
    /// <param name="count">The number of pairs.</param>
    /// <returns>The pairs as JSON text, expected first.</returns>
    internal static IReadOnlyList<(string Expected, string Actual)> Generate(ulong seed, int count)
    {
        var random = new SplitMix64(seed);
        var pairs = new List<(string, string)>(count);
        for (var i = 0; i < count; i++)
        {
            var expected = Node(random, 0);
            var actual = expected.Copy();
            var mutations = 1 + random.Next(3);
            for (var m = 0; m < mutations; m++)
            {
                Mutate(random, actual);
            }

            pairs.Add((expected.Render(), actual.Render()));
        }

        return pairs;
    }

    private static JsonNodeModel Node(SplitMix64 random, int depth)
    {
        var pick = depth >= 3 ? 9 : random.Next(10);
        return pick switch
        {
            < 3 => ObjectNode(random, depth),
            < 5 => ArrayNode(random, depth),
            _ => Scalar(random),
        };
    }

    private static JsonNodeModel ObjectNode(SplitMix64 random, int depth)
    {
        var node = new JsonNodeModel(JsonNodeKind.Object);
        foreach (var name in Names.Where(_ => random.Next(2) is 0).Take(4))
        {
            node.Properties.Add((name, Node(random, depth + 1)));
        }

        return node;
    }

    private static JsonNodeModel ArrayNode(SplitMix64 random, int depth)
    {
        var node = new JsonNodeModel(JsonNodeKind.Array);
        var length = random.Next(5);
        for (var i = 0; i < length; i++)
        {
            node.Items.Add(Node(random, depth + 1));
        }

        return node;
    }

    private static JsonNodeModel Scalar(SplitMix64 random) => random.Next(6) switch
    {
        0 or 1 => JsonNodeModel.Number(random.Next(Numbers.Length), random),
        2 => JsonNodeModel.Literal("\"" + Strings[random.Next(Strings.Length)] + "\""),
        3 => JsonNodeModel.Literal("true"),
        4 => JsonNodeModel.Literal("false"),
        _ => JsonNodeModel.Literal("null"),
    };

    private static void Mutate(SplitMix64 random, JsonNodeModel root)
    {
        var nodes = new List<JsonNodeModel>();
        root.Collect(nodes);
        var target = nodes[random.Next(nodes.Count)];
        switch (random.Next(8))
        {
            case 0:
                target.Reform(random);
                break;
            case 1:
                target.Shuffle(random);
                break;
            case 2:
                target.ChangeValue(random);
                break;
            case 3:
                target.RemoveOne(random);
                break;
            case 4:
                target.AddOne(random, Node(random, 2));
                break;
            case 5:
                target.ReplaceWith(Node(random, 2));
                break;
            case 6:
                target.FlipBoolean();
                break;
            default:
                root.Reform(random);
                root.Shuffle(random);
                break;
        }
    }

    private enum JsonNodeKind
    {
        Object,
        Array,
        Scalar,
    }

    /// <summary>A mutable JSON tree that renders its own number forms and property order.</summary>
    private sealed class JsonNodeModel(JsonNodeKind kind)
    {
        private JsonNodeKind _kind = kind;
        private string _raw = string.Empty;
        private int _numberRow = -1;

        public List<(string Name, JsonNodeModel Value)> Properties { get; } = [];

        public List<JsonNodeModel> Items { get; } = [];

        public static JsonNodeModel Literal(string raw) => new(JsonNodeKind.Scalar) { _raw = raw };

        public static JsonNodeModel Number(int row, SplitMix64 random)
        {
            var forms = Numbers[row];
            return new JsonNodeModel(JsonNodeKind.Scalar) { _numberRow = row, _raw = forms[random.Next(forms.Length)] };
        }

        public JsonNodeModel Copy()
        {
            var copy = new JsonNodeModel(_kind) { _raw = _raw, _numberRow = _numberRow };
            copy.Properties.AddRange(Properties.Select(p => (p.Name, p.Value.Copy())));
            copy.Items.AddRange(Items.Select(i => i.Copy()));
            return copy;
        }

        public void Collect(List<JsonNodeModel> nodes)
        {
            nodes.Add(this);
            Properties.ForEach(p => p.Value.Collect(nodes));
            Items.ForEach(i => i.Collect(nodes));
        }

        public void Reform(SplitMix64 random)
        {
            if (_numberRow >= 0)
            {
                var forms = Numbers[_numberRow];
                _raw = forms[random.Next(forms.Length)];
            }

            Properties.ForEach(p => p.Value.Reform(random));
            Items.ForEach(i => i.Reform(random));
        }

        public void Shuffle(SplitMix64 random)
        {
            ShuffleList(Properties, random);
            ShuffleList(Items, random);
        }

        public void ChangeValue(SplitMix64 random)
        {
            if (_numberRow >= 0)
            {
                var row = (_numberRow + 1 + random.Next(Numbers.Length - 1)) % Numbers.Length;
                var forms = Numbers[row];
                (_numberRow, _raw) = (row, forms[random.Next(forms.Length)]);
            }
            else if (_kind is JsonNodeKind.Scalar && _raw.StartsWith('"'))
            {
                _raw = string.Equals(_raw, "\"x\"", StringComparison.Ordinal) ? "\"y\"" : "\"x\"";
            }
        }

        public void RemoveOne(SplitMix64 random)
        {
            if (Properties.Count > 0)
            {
                Properties.RemoveAt(random.Next(Properties.Count));
            }
            else if (Items.Count > 0)
            {
                Items.RemoveAt(random.Next(Items.Count));
            }
        }

        public void AddOne(SplitMix64 random, JsonNodeModel value)
        {
            if (_kind is JsonNodeKind.Object)
            {
                var free = Names.Where(n => Properties.TrueForAll(p => !string.Equals(p.Name, n, StringComparison.Ordinal))).ToList();
                if (free.Count > 0)
                {
                    Properties.Insert(random.Next(Properties.Count + 1), (free[random.Next(free.Count)], value));
                }
            }
            else if (_kind is JsonNodeKind.Array)
            {
                Items.Insert(random.Next(Items.Count + 1), value);
            }
        }

        public void ReplaceWith(JsonNodeModel other)
        {
            (_kind, _raw, _numberRow) = (other._kind, other._raw, other._numberRow);
            Properties.Clear();
            Properties.AddRange(other.Properties);
            Items.Clear();
            Items.AddRange(other.Items);
        }

        public void FlipBoolean()
        {
            _raw = _raw switch
            {
                "true" => "false",
                "false" => "true",
                _ => _raw,
            };
        }

        public string Render()
        {
            var sb = new StringBuilder();
            RenderTo(sb);
            return sb.ToString();
        }

        private static void ShuffleList<T>(List<T> list, SplitMix64 random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void RenderTo(StringBuilder sb)
        {
            switch (_kind)
            {
                case JsonNodeKind.Object:
                    sb.Append('{');
                    for (var i = 0; i < Properties.Count; i++)
                    {
                        sb.Append(i > 0 ? "," : string.Empty).Append('"').Append(Properties[i].Name).Append("\":");
                        Properties[i].Value.RenderTo(sb);
                    }

                    sb.Append('}');
                    break;
                case JsonNodeKind.Array:
                    sb.Append('[');
                    for (var i = 0; i < Items.Count; i++)
                    {
                        sb.Append(i > 0 ? "," : string.Empty);
                        Items[i].RenderTo(sb);
                    }

                    sb.Append(']');
                    break;
                default:
                    sb.Append(_raw);
                    break;
            }
        }
    }

    /// <summary>SplitMix64: a small, fully specified generator, so a seed gives the same sequence everywhere.</summary>
    private sealed class SplitMix64(ulong seed)
    {
        private ulong _state = seed;

        public int Next(int exclusiveMax)
        {
            _state += 0x9E3779B97F4A7C15UL;
            var z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (int)(z % (ulong)exclusiveMax);
        }
    }
}
