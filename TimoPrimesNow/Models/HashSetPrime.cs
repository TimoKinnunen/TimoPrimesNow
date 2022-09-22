using System.Numerics;

namespace TimoPrimesNow.Models
{
    public class HashSetPrime
    {
        public HashSetPrime() { }

        public HashSetPrime(int id, BigInteger primeNumber)
        {
            Id = id;
            Primenumber = primeNumber;
        }

        public int Id { get; set; }

        public BigInteger Primenumber { get; set; }
    }
}
