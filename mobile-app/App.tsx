import React, { useState, useEffect } from 'react';
import { StyleSheet, View, Text, TextInput, TouchableOpacity, ScrollView, Alert } from 'react-native';
import { supabase } from './src/config/supabase';

interface MarketRates {
  Blue: number;
  Purple: number;
  Yellow: number;
  Green: number;
  Dollar: number;
}

interface PlayerData {
  id: number;
  Player: string;
  Dollar: number;
  Blue: number;
  Purple: number;
  Yellow: number;
  Green: number;
}

export default function App() {
  const [playerName, setPlayerName] = useState('');
  const [playerId, setPlayerId] = useState<number | null>(null);
  const [marketRates, setMarketRates] = useState<MarketRates | null>(null);
  const [playerData, setPlayerData] = useState<PlayerData | null>(null);

  useEffect(() => {
    fetchMarketRates();
  }, []);

  const fetchMarketRates = async () => {
    const { data, error } = await supabase
      .from('AiPoopers')
      .select('*')
      .eq('Player', 'Market')
      .single();

    if (error) {
      Alert.alert('Error', 'Failed to fetch market rates');
      return;
    }

    setMarketRates(data);
  };

  const registerPlayer = async () => {
    if (!playerName.trim()) {
      Alert.alert('Error', 'Please enter a player name');
      return;
    }

    const { data: existingPlayer } = await supabase
      .from('AiPoopers')
      .select('*')
      .eq('Player', playerName)
      .single();

    if (existingPlayer) {
      setPlayerId(existingPlayer.id);
      setPlayerData(existingPlayer);
      return;
    }

    const { data, error } = await supabase
      .from('AiPoopers')
      .insert([
        {
          Player: playerName,
          Dollar: 100,
          Blue: 0,
          Purple: 0,
          Yellow: 0,
          Green: 0
        }
      ])
      .select()
      .single();

    if (error) {
      Alert.alert('Error', 'Failed to register player');
      return;
    }

    setPlayerId(data.id);
    setPlayerData(data);
  };

  const handleTrade = async (color: 'Blue' | 'Purple' | 'Yellow' | 'Green', isBuying: boolean) => {
    if (!playerId || !marketRates || !playerData) return;

    await fetchMarketRates(); // Get latest rates
    const rate = marketRates[color];
    const amount = 1; // Trade 1 unit at a time

    if (isBuying) {
      const cost = rate * amount;
      if (playerData.Dollar < cost) {
        Alert.alert('Error', 'Insufficient dollars');
        return;
      }

      const { error } = await supabase
        .from('AiPoopers')
        .update({
          Dollar: playerData.Dollar - cost,
          [color]: playerData[color] + amount
        })
        .eq('id', playerId);

      if (error) {
        Alert.alert('Error', 'Failed to buy');
        return;
      }

      setPlayerData({
        ...playerData,
        Dollar: playerData.Dollar - cost,
        [color]: playerData[color] + amount
      });
    } else {
      if (playerData[color] < amount) {
        Alert.alert('Error', `Insufficient ${color} balance`);
        return;
      }

      const earnings = rate * amount;
      const { error } = await supabase
        .from('AiPoopers')
        .update({
          Dollar: playerData.Dollar + earnings,
          [color]: playerData[color] - amount
        })
        .eq('id', playerId);

      if (error) {
        Alert.alert('Error', 'Failed to sell');
        return;
      }

      setPlayerData({
        ...playerData,
        Dollar: playerData.Dollar + earnings,
        [color]: playerData[color] - amount
      });
    }
  };

  const renderTradeButtons = (color: 'Blue' | 'Purple' | 'Yellow' | 'Green') => (
    <View style={styles.tradeContainer}>
      <Text style={styles.colorText}>{color}</Text>
      <Text>Rate: ${marketRates?.[color].toFixed(2)}</Text>
      <Text>Balance: {playerData?.[color].toFixed(2)}</Text>
      <View style={styles.buttonContainer}>
        <TouchableOpacity
          style={[styles.button, styles.buyButton]}
          onPress={() => handleTrade(color, true)}
        >
          <Text style={styles.buttonText}>Buy</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.button, styles.sellButton]}
          onPress={() => handleTrade(color, false)}
        >
          <Text style={styles.buttonText}>Sell</Text>
        </TouchableOpacity>
      </View>
    </View>
  );

  if (!playerId) {
    return (
      <View style={styles.container}>
        <Text style={styles.title}>Currency Trading App</Text>
        <TextInput
          style={styles.input}
          placeholder="Enter player name"
          value={playerName}
          onChangeText={setPlayerName}
        />
        <TouchableOpacity style={styles.registerButton} onPress={registerPlayer}>
          <Text style={styles.buttonText}>Register</Text>
        </TouchableOpacity>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>Welcome {playerName}!</Text>
      <Text style={styles.balance}>Balance: ${playerData?.Dollar.toFixed(2)}</Text>
      {['Blue', 'Purple', 'Yellow', 'Green'].map((color) => (
        renderTradeButtons(color as 'Blue' | 'Purple' | 'Yellow' | 'Green')
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    backgroundColor: '#f5f5f5',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    marginTop: 40,
    marginBottom: 20,
    textAlign: 'center',
  },
  input: {
    height: 50,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    paddingHorizontal: 15,
    marginBottom: 20,
    backgroundColor: 'white',
  },
  registerButton: {
    backgroundColor: '#4CAF50',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
  },
  balance: {
    fontSize: 20,
    fontWeight: 'bold',
    marginBottom: 20,
    textAlign: 'center',
  },
  tradeContainer: {
    backgroundColor: 'white',
    padding: 15,
    borderRadius: 8,
    marginBottom: 15,
    elevation: 2,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
  },
  colorText: {
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 5,
  },
  buttonContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 10,
  },
  button: {
    flex: 1,
    padding: 10,
    borderRadius: 6,
    alignItems: 'center',
    marginHorizontal: 5,
  },
  buyButton: {
    backgroundColor: '#4CAF50',
  },
  sellButton: {
    backgroundColor: '#f44336',
  },
  buttonText: {
    color: 'white',
    fontWeight: 'bold',
  },
});
