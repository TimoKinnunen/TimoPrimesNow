using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimoPrimesNow.Models;

namespace TimoPrimesNow.Repository
{
    public class SqlRepository
    {
        SQLiteAsyncConnection db { get; set; }

        public SqlRepository()
        {
            db = new SQLiteAsyncConnection(App.DatabasePath);

            Task result = Task.Run(async () => await CreateTableAsync());

            if (result != null)
            {
                Task anotherResult = Task.Run(async () => await SeedMethodAsync());
            }
        }

        private async Task SeedMethodAsync()
        {
            if (await GetPrimesCountAsync() == 0)
            {
                await UpsertPrimeAsync(new Prime { Primenumber = 2.ToString() });
                await UpsertPrimeAsync(new Prime { Primenumber = 3.ToString() });
                await UpsertPrimeAsync(new Prime { Primenumber = 5.ToString() });
            }
        }

        public async Task CreateTableAsync()
        {
            CreateTableResult createTableResult = await db.CreateTableAsync<Prime>();
            switch (createTableResult)
            {
                case CreateTableResult.Created:
                    break;
                case CreateTableResult.Migrated:
                    break;
                default:
                    break;
            }
        }

        public async Task<Prime> SearchPrimeAsync(string primeNumber)
        {
            return await db.Table<Prime>().FirstOrDefaultAsync(p => p.Primenumber == primeNumber).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Prime>> SearchAllPrimesAsync(string firstLetterString)
        {
            return await db.Table<Prime>().Where(p => p.Primenumber.StartsWith(firstLetterString)).OrderBy(p => p.Id).ToListAsync().ConfigureAwait(false);
        }

        public async Task<int> UpsertPrimeAsync(Prime prime)
        {
            if (prime.Id == 0)
            {
                var added = await db.InsertAsync(prime).ConfigureAwait(false);
                return prime.Id;
            }
            else
            {
                return await db.UpdateAsync(prime).ConfigureAwait(false);
            }
        }

        public async Task<Prime> GetPrimeAsync(int id)
        {
            return await db.Table<Prime>().FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Prime>> GetAllPrimesAsync(int id = 0, int takeNumberOfRecords = 100000)
        {
            //takeNumberOfRecords should be less than 1000000 or you get OutOfMemoryException
            //without .Take(takeNumberOfRecords) you get OutOfMemoryException
            return await db.Table<Prime>().Where(p => p.Id > id).OrderBy(p => p.Id).Take(takeNumberOfRecords).ToListAsync().ConfigureAwait(false);
        }

        public async Task<int> GetPrimesCountAsync()
        {
            return await db.Table<Prime>().CountAsync().ConfigureAwait(false);
        }

        public async Task<string> GetBiggestPrimenumberAsStringAsync()
        {
            // Reverse the order and take first, it is now the last item
            Prime primeNumberItem = await db.Table<Prime>().OrderByDescending(p => p.Id).FirstOrDefaultAsync().ConfigureAwait(false);
            if (primeNumberItem != null)
            {
                return primeNumberItem.Primenumber;
            }
            else
            {
                return "0";
            }
        }

        public async Task<Prime> GetBiggestPrimeAsync()
        {
            // Reverse the order and take first, it is now the last item
            return await db.Table<Prime>().OrderByDescending(p => p.Id).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<int> DeleteDatabaseTableAsync()
        {
            return await db.DropTableAsync<Prime>();
        }
    }
}
