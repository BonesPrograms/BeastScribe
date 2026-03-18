using BeastScribe;
using BeastScribe.Scribes;
using System;

namespace BeastScribe
{
    public static class Scribe
    {
        public static ScribeWriter Writer = ScribeWriter.Writer;
        public static ScribeReader Reader = ScribeReader.Reader;
    }

}

namespace XRL.World.Parts
{

    [Serializable]
    public abstract class IBeastScribedPart : IPart
    {
        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Scribe.Writer.Scribe(Writer, this);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            Scribe.Reader.Scribe(Reader, this);
        }
    }
}

namespace XRL.World.Effects
{

    [Serializable]
    public abstract class IBeastScribedEffect : Effect
    {
        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Scribe.Writer.Scribe(Writer, this);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            Scribe.Reader.Scribe(Reader, this);
        }

    }
}

namespace XRL.World
{
    [Serializable]
    public abstract class IBeastScribedComposite : IComposite
    {

        public bool WantFieldReflection => false;
        public virtual void Write(SerializationWriter Writer)
        {
            Scribe.Writer.Scribe(Writer, this);
        }

        public virtual void Read(SerializationReader Reader)
        {
            Scribe.Reader.Scribe(Reader, this);
        }
    }
}