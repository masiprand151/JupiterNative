import {
  Button,
  Image,
  SafeAreaView,
  Text,
  View,
  useEffect,
  useState,
} from "@jupiter/core";

export default function HomeScreen() {
  const [count, setCount] = useState(0);

  return (
    <SafeAreaView>
      <View
        padding={24}
        gap={12}
        direction="vertical"
        style={{
          height: 764,

          justifyContent: "center",
          backgroundColor: "#1773c8",
        }}
      >
        <Image
          src="assets/jn.png"
          alt="JN logo"
          width={64}
          height={64}
          objectFit="contain"
        />
        <View
          gap={20}
          style={{
            width: 400,
            alignItems: "center",
          }}
        >
          <Text fontSize={28} fontWeight="bold">
            Welcome to Jupiter Native
          </Text>
          <Text>
            Build native Windows desktop apps with TypeScript and TSX.
          </Text>
          <Text>Counter: {count}</Text>
          <Button
            title="Increment counter"
            onPress={() => setCount((value) => value + 1)}
          />
        </View>
      </View>
    </SafeAreaView>
  );
}
