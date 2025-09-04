namespace BPEngine.Generation
{
    public interface IConstrainedDecoder
    {
        bool Accept(string tokenPiece); // would appending this keep a valid JSON prefix?
        void Push(string tokenPiece);   // commit append
        void Reset();
    }

}
