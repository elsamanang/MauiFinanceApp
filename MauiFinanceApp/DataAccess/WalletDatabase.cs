﻿using MauiFinanceApp.Models;
using MauiFinanceApp.Utils;
using SQLite;

namespace MauiFinanceApp.DataAccess
{
    public class WalletDatabase
    {
        SQLiteAsyncConnection Database;

        public WalletDatabase()
        {
            InitAsync();
        }

        /// <summary>
        /// Init database by creating or not the table if it's already done.
        /// </summary>
        async void InitAsync()
        {
            if (Database is not null)
                return;

            Database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);

            await Database.CreateTableAsync<User>();
            await Database.CreateTableAsync<Card>();
            await Database.CreateTableAsync<Operation>();
        }

        #region UserOps

        public async ValueTask SaveUserAsync(User user)
            => await Database.InsertOrReplaceAsync(user);

        public async ValueTask<User> GetConnectedUserAsync()
            => await Database.Table<User>().FirstOrDefaultAsync();

        public async ValueTask<bool> RemoveUserAsync()
        {
            var user = await Database.Table<User>().FirstOrDefaultAsync();
            if (user is not null)
            {
                var result = await Database.DeleteAsync(user);
                return result > 0;
            }

            return false;
        }
        #endregion

        #region CardOps

        public async ValueTask SaveCardAsync(Card card)
            => await Database.InsertOrReplaceAsync(card);

        public async ValueTask<IEnumerable<Card>> GetCardsAsync()
            => await Database.Table<Card>().Where(x => x.IsActive).ToListAsync();

        public async ValueTask RemoveCardAsync(int cardId)
            => await Database.DeleteAsync(cardId);

        #endregion

        #region OperationOps
        public async ValueTask SaveOperationAsync(Operation operation)
           => await Database.InsertOrReplaceAsync(operation);

        public async ValueTask<IEnumerable<Operation>> GetOperationListByCardAsync(int card)
            => await Database.Table<Operation>().Where(x => x.IsDeleted == false && x.CardId == card)
                .OrderByDescending(x => x.OperationDate)
                .ToListAsync();

        public async ValueTask<Operation> RemoveOperationAsync(int operationId)
        {
            var ops = await Database.Table<Operation>()
                .FirstOrDefaultAsync(o => o.Id == operationId && o.IsDeleted == false);
            if (ops is null)
            {
                return null;
            }

            var card = await Database.Table<Card>().FirstOrDefaultAsync(c => c.Id == ops.CardId && c.IsActive);
            if (card is null)
            {
                return null;
            }

            card.Amount += ops.Amount;
            ops.IsDeleted = true;

            await Database.InsertOrReplaceAsync(card);
            await Database.InsertOrReplaceAsync(ops);

            return ops;
        }
        #endregion

    }

}
