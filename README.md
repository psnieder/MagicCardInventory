Inventory MTG cards in a local SQL server database. Utilizes the scryfall API to get card information (https://scryfall.com/docs/api).

# Command Line Arguments: 

	0 - Function to run. Current functions are Add and UpdatePrices. Add will inventory a new card, and UpdatePrices will update the prices of all inventoried cards
		Add - command line argument 1 is required
		UpdatePrices - no additional command line arguments are required.
	1 - Scryfall card ID from scryfall API found at https://scryfall.com. A comma separated list of IDs can be passed to inventory multiple cards at a time. Arguments 2 and 3 cannot be used with this option.
	2 - Optional - If the card is a foil or not. Use "yes" if the card is a foil.
	3 - Optional - Number of cards to add. Will add one card if not passed in.

# Database tables:
	View the SQL class to see database tables and structure. Primary keys on card tables should be card_id or card_id/sequence for tables that contain a sequence.