import {
  Button,
  Image,
  NavigationScreenProps,
  SafeAreaView,
  Text,
  TextInput,
  View,
  desktop,
  navigate,
  useState,
  Video,
} from "@jupiter/core";

export default function HomeScreen() {
  const [count, setCount] = useState(0);
  const [name, setName] = useState("");

  const a = Array.from({ length: 20 }).map((_, i) => "Nomor " + i);

  return (
    <SafeAreaView>
      <View padding={24} gap={12}>
        <Image
          src="assets/jn.png"
          alt="JN logo"
          width={64}
          height={64}
          objectFit="contain"
        />
        <Text fontSize={28} fontWeight="bold">
          Welcome to Jupiter Native
        </Text>
        <Text>Build native Windows desktop apps with TypeScript and TSX.</Text>

        <TextInput
          id="home-name-input"
          placeholder="Enter your name"
          value={name}
          onChangeText={setName}
        />
        <Text>
          {name ? `Hello, ${name}!` : "Enter your name to get started."}
        </Text>

        <Text>Counter: {count}</Text>
        <Button
          title="Increment counter"
          onPress={() => setCount((value) => value + 1)}
        />
      </View>
    </SafeAreaView>
  );
}
