import type { Card, GameMode, CardPlay } from '../types/Card';

export class CardService {
    private static readonly SUITS: Card['suit'][] = ['♠', '♥', '♦', '♣'];
    private static readonly VALUES: Card['value'][] = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

    /**
     * Crea un mazo de naipes españoles (40 cartas)
     */
    static createSpanishDeck(): Card[] {
        const deck: Card[] = [];

        for (const suit of this.SUITS) {
            for (const value of this.VALUES) {
                deck.push({
                    suit,
                    value,
                    id: `${suit}-${value}-${Math.random().toString(36).substr(2, 9)}`,
                    isPepinoOro: suit === '♦' && value === 3 // El 3 de oro es el pepino de oro
                });
            }
        }

        return deck;
    }

    /**
     * Crea múltiples mazos para el juego
     */
    static createMultipleDecks(deckCount: number): Card[] {
        const allCards: Card[] = [];

        for (let i = 0; i < deckCount; i++) {
            const deck = this.createSpanishDeck();
            // Agregar sufijo al ID para distinguir mazos
            deck.forEach(card => {
                card.id = `${card.id}-deck${i}`;
            });
            allCards.push(...deck);
        }

        return allCards;
    }

    /**
     * Baraja el mazo de cartas
     */
    static shuffleDeck(deck: Card[]): Card[] {
        const shuffled = [...deck];
        for (let i = shuffled.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]];
        }
        return shuffled;
    }

    /**
     * Calcula el modo de juego basado en la cantidad de jugadores
     */
    static calculateGameMode(playerCount: number): GameMode {
        let deckCount: number;
        let maxWinners: number;

        if (playerCount <= 4) {
            deckCount = Math.min(2, Math.max(1, Math.ceil(40 / playerCount))); // Máximo 2 mazos
            maxWinners = 2;
        } else {
            deckCount = Math.min(3, Math.max(1, Math.ceil(40 / playerCount))); // Máximo 3 mazos
            maxWinners = 3;
        }

        const totalCards = deckCount * 40;
        const cardsPerPlayer = Math.floor(totalCards / playerCount);

        return {
            deckCount,
            maxWinners,
            cardsPerPlayer
        };
    }

    /**
     * Reparte todas las cartas a los jugadores
     */
    static dealAllCards(deck: Card[], numPlayers: number): {
        hands: Card[][];
        remainingDeck: Card[];
    } {
        const hands: Card[][] = Array(numPlayers).fill(null).map(() => []);
        const remainingDeck = [...deck];

        // Repartir todas las cartas
        let currentPlayer = 0;
        while (remainingDeck.length > 0) {
            const card = remainingDeck.pop()!;
            hands[currentPlayer].push(card);
            currentPlayer = (currentPlayer + 1) % numPlayers;
        }

        return { hands, remainingDeck };
    }

    /**
     * Obtiene el valor numérico de una carta para comparaciones
     * En Pepino: 3 < 4 < 5 < ... < 12 < 1, el 2 es comodín
     */
    static getCardValue(card: Card): number {
        if (card.value === 2) return 0; // Comodín
        if (card.value === 1) return 13; // El 1 es el más alto
        return card.value; // 3-12 mantienen su valor
    }

    /**
     * Obtiene el color de la carta para estilos CSS
     */
    static getCardColor(card: Card): string {
        return card.suit === '♥' || card.suit === '♦' ? 'red' : 'black';
    }

    /**
     * Valida si una jugada es válida según las reglas del Pepino
     */
    static validatePlay(selectedCards: Card[], lastPlayedCards: Card[] | null, isFirstPlay: boolean): CardPlay {
        // Verificar que todas las cartas tengan el mismo valor
        if (selectedCards.length === 0) {
            return { cards: selectedCards, playerId: '', isValid: false, reason: 'Debes seleccionar al menos una carta' };
        }

        const firstValue = selectedCards[0].value;
        const allSameValue = selectedCards.every(card => card.value === firstValue);

        if (!allSameValue) {
            return { cards: selectedCards, playerId: '', isValid: false, reason: 'Todas las cartas deben tener el mismo valor' };
        }

        // Si es la primera jugada, cualquier carta es válida
        if (isFirstPlay || !lastPlayedCards || lastPlayedCards.length === 0) {
            return { cards: selectedCards, playerId: '', isValid: true };
        }

        // Verificar que la cantidad de cartas sea la misma
        if (selectedCards.length !== lastPlayedCards.length) {
            return { cards: selectedCards, playerId: '', isValid: false, reason: `Debes jugar ${lastPlayedCards.length} carta(s)` };
        }

        // Verificar que el valor sea mayor O IGUAL (para PEPINEADO)
        const lastValue = this.getCardValue(lastPlayedCards[0]);
        const currentValue = this.getCardValue(selectedCards[0]);

        // Debug logs para entender qué está pasando
        console.log('🔍 Validación de jugada:');
        console.log(`   📋 Cartas seleccionadas: ${selectedCards.map(c => `${c.value}${c.suit}`).join(', ')}`);
        console.log(`   📋 Última jugada: ${lastPlayedCards.map(c => `${c.value}${c.suit}`).join(', ')}`);
        console.log(`   🎯 Valor actual: ${currentValue} (carta: ${selectedCards[0].value}${selectedCards[0].suit})`);
        console.log(`   🎯 Valor anterior: ${lastValue} (carta: ${lastPlayedCards[0].value}${lastPlayedCards[0].suit})`);
        console.log(`   ✅ ¿Es válida? ${currentValue >= lastValue ? 'SÍ' : 'NO'}`);

        if (currentValue < lastValue) {
            return { cards: selectedCards, playerId: '', isValid: false, reason: 'Debes jugar cartas de mayor o igual valor' };
        }

        return { cards: selectedCards, playerId: '', isValid: true };
    }

    /**
     * Verifica si una jugada es "PEPINEADO" (misma jugada que la anterior)
     */
    static isPepineado(selectedCards: Card[], lastPlayedCards: Card[]): boolean {
        if (!lastPlayedCards || lastPlayedCards.length === 0) return false;

        if (selectedCards.length !== lastPlayedCards.length) return false;

        // Verificar que todas las cartas tengan el mismo valor
        const selectedValue = selectedCards[0].value;
        const lastValue = lastPlayedCards[0].value;

        return selectedValue === lastValue && selectedCards.every(card => card.value === selectedValue);
    }

    /**
     * Encuentra el jugador con el pepino de oro (3 de oro)
     */
    static findPepinoOroPlayer(hands: Card[][]): number {
        for (let i = 0; i < hands.length; i++) {
            const hasPepinoOro = hands[i].some(card => card.isPepinoOro);
            if (hasPepinoOro) {
                return i;
            }
        }
        return 0; // Por defecto, empieza el primer jugador
    }

    /**
     * Obtiene el nombre de la carta para mostrar
     */
    static getCardDisplayName(card: Card): string {
        const valueNames: Record<Card['value'], string> = {
            1: 'As',
            2: '2',
            3: '3',
            4: '4',
            5: '5',
            6: '6',
            7: '7',
            8: '8',
            9: '9',
            10: '10',
            11: '11',
            12: '12'
        };

        const valueName = valueNames[card.value];
        const suitName = card.suit;

        if (card.isPepinoOro) {
            return `${valueName}${suitName} (Pepino de Oro)`;
        }

        return `${valueName}${suitName}`;
    }
} 