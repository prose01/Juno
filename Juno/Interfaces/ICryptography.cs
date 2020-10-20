namespace Juno.Interfaces
{
    public interface ICryptography
    {
        string Encrypt(string PlainText);
        string Decrypt(string CipherText);
    }
}
