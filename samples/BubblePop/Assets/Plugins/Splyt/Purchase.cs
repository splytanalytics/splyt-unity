using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Splyt.Plugins
{
	/// <summary>
	/// A light wrapper around Splyt.Transaction to provide some built-in characteristics for Purchase transactions
	/// </summary>
	public class PurchaseTransaction : TransactionBase<PurchaseTransaction>
	{
		internal PurchaseTransaction(string transactionId) : base("Purchase", transactionId) {}

		/// <summary>
		/// Reports the price for the item being purchased
		/// </summary>
		/// <returns>The transaction itself (to support a builder-style implementation)</returns>
		/// <param name="amount">How much currency</param>
		/// <param name="currency">
		/// 	For real currency purchases, the ISO 4217 currency code (e.g., "USD") that applies to (amount). This is NOT case sensitive
		///     Or if that is unknown, pass currency symbol (e.g., "$", "€", "£", etc.) and we will attempt to determine the correct currency for you.
		///    	For virtual currency purchases, this is the name of the virtual currency used to make the purchase (e.g., "coins", "gems", etc.)
		///		NOTE:  Only ASCII characters are supported for virtual currencies.  Any non-ASCII characters are stripped.
		/// </param>
		public PurchaseTransaction setPrice(double amount, string currency)
		{
			#if UNITY_IPHONE && !UNITY_EDITOR
			currency = splyt_util_getvalidcurrencystring(currency);
			#elif UNITY_ANDROID && !UNITY_EDITOR
			currency = Splyt.Core.callNative<string>("splyt_util_getvalidcurrencystring", new object[] {currency});
			#else
			currency = Util.getValidCurrencyString(currency);
			#endif

			setProperty("price", new Dictionary<string, object> { { currency, amount } });
			/* ONCE WE ADD LIST SUPPORT TO THE DATA COLLECTOR, SWITCH OUT THIS BLOCK
			if(!_state.ContainsKey("price")) _state.Add("price", new ArrayList());

			ArrayList prices = _state["price"];
			prices.Add( new Dictionary<string, object> { { "currency", currency }, { "amount", amount } } );
			*/

			return this;
		}

		/// <summary>
		/// Reports an offer id for the item being purchased. Useful for identifying promotions or other application defined offers.
		/// </summary>
		/// <returns>The transaction itself (to support a builder-style implementation)</returns>
		/// <param name="offerId">The offer id</param>
		public PurchaseTransaction setOfferId(string offerId) {	setProperty("offerId", offerId); return this; }

		/// <summary>
		/// Reports a name for the item.
		/// </summary>
		/// <returns>The transaction itself (to support a builder-style implementation)</returns>
		/// <param name="itemName">An item name</param>
		public PurchaseTransaction setItemName(string itemName) { setProperty("itemName", itemName); return this; }

		/// <summary>
		/// Reports the point of sale. Useful in situations where an application may have multiple points-of-purchase.
		/// </summary>
		/// <returns>The transaction itself (to support a builder-style implementation)</returns>
		/// <param name="pointOfSale">Application defined point-of-sale</param>
		public PurchaseTransaction setPointOfSale(string pointOfSale) { setProperty("pointOfSale", pointOfSale); return this; }

		#if UNITY_IPHONE && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern string splyt_util_getvalidcurrencystring(string currency);
		#endif
	}

	/// <summary>
	/// This Splyt plugin provides a simple interface for instrumenting purchase flows in an application.
	/// </summary>
	public class Purchase
	{
		/// <summary>
		/// Factory method for invoking SplytPlugins.SessionTransaction methods
		/// </summary>
		/// <param name="transactionId">Transaction id, if applicable - this is only REQUIRED in situation where multiple transactions in the same category may exist (read: be concurrently begun)</param> 
		public static PurchaseTransaction Transaction(string transactionId = null)
		{
			return new PurchaseTransaction(transactionId);
		}
	}
}

