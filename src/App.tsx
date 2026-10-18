import { NavigationContainer, Window } from "@jupiter/core";
import HomeScreen, { ProfileScreen } from "./screens/HomeScreen";

export default function App() {
  return (
    <Window>
      <NavigationContainer
        initialRouteName="Home"
        screens={{
          Home: HomeScreen,
          Profile: ProfileScreen,
        }}
      />
    </Window>
  );
}
