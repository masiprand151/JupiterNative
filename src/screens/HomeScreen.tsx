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
} from "@jupiter/core";

export default function HomeScreen() {
  const [count, setCount] = useState(0);
  const [name, setName] = useState("");

  return (
    <SafeAreaView>
      <View padding={24} gap={12}>
        <Image src="assets/jn.png" alt="JN logo" width={64} height={64} objectFit="contain" />
        <Text fontSize={28} fontWeight="bold">Welcome to Jupiter</Text>
        <Text>Build native Windows desktop apps with TypeScript and TSX.</Text>

        <TextInput
          id="home-name-input"
          placeholder="Enter your name"
          value={name}
          onChangeText={setName}
        />
        <Text>{name ? `Hello, ${name}!` : "Enter your name to get started."}</Text>

        <Text>Counter: {count}</Text>
        <Button title="Increment counter" onPress={() => setCount((value) => value + 1)} />
        <Button title="Open profile" onPress={() => navigate("Profile", { name, count })} />
        <Button title="Open documentation" onPress={() => desktop.openUrl("docs/index.html")} />
      </View>
    </SafeAreaView>
  );
}

type ProfileParams = { name?: string; count?: number };

export function ProfileScreen({ route, navigation }: NavigationScreenProps) {
  const params = (route.params ?? {}) as ProfileParams;

  return (
    <SafeAreaView>
      <View padding={24} gap={12}>
        <Text fontSize={24} fontWeight="bold">Profile</Text>
        <Text>Name: {params.name || "Guest"}</Text>
        <Text>Counter received: {params.count ?? 0}</Text>
        <Button title="Go back" onPress={navigation.goBack} />
      </View>
    </SafeAreaView>
  );
}
