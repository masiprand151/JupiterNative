import { NavigationContainer, Window } from "@jupiter/core";
import HomeScreen from "./screens/HomeScreen";

export default function App() {
  return (
    <Window>
      <NavigationContainer
        initialRouteName="Home"
        screens={{
          Home: HomeScreen,
        }}
      />
    </Window>
  );
}
