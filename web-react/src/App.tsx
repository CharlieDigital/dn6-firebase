import { initializeApp } from "firebase/app";
import {
  getAuth,
  signInWithPopup,
  GoogleAuthProvider,
  connectAuthEmulator,
} from "firebase/auth";
import {
  collection,
  connectFirestoreEmulator,
  getFirestore,
  onSnapshot,
  Unsubscribe,
} from "firebase/firestore";

import { useCallback, useEffect, useMemo, useState } from 'react'
import './App.css'

interface City {
  State: string
  Name: string
}

const firebaseConfig = {
  apiKey: "AIzaSyBa_VDckwNQ2OaooVRoSJY",
  authDomain: "dn6-firebase-demo.firebaseapp.com",
  projectId: "dn6-firebase-demo",
  storageBucket: "dn6-firebase-demo.appspot.com",
  messagingSenderId: "87796597610",
  appId: "1:87796597610:web:c8d363161b2ead61811b13",
};

const app = initializeApp(firebaseConfig);
const provider = new GoogleAuthProvider();
const auth = getAuth(app);
connectAuthEmulator(auth, "http://localhost:10099");

const db = getFirestore(app);
connectFirestoreEmulator(db, "localhost", 8181);

function App() {
  const [cityName, setCityName] = useState('')
  const [stateName, setStateName] = useState('')
  const [authToken, setAuthToken] = useState('')
  const [cities, setCities] = useState<City[]>([])
  const [started, setStarted] = useState<boolean>()
  const [, updateState] = useState();
  const forceUpdate = useCallback(() => updateState({} as any), []);

  /**
   * Function to create a city
   */
  const createCity = async () => {
    const city = cityName;
    const state = stateName;
    const url = `http://localhost:20080/city/add/${state}/${city}`;

    const response = await fetch(url, {
      method: "GET",
      headers: new Headers({
        Authorization: "bearer " + authToken,
      }),
    });

    console.log(response);
  };

  /**
   * Function to perform authentication
   */
  const firebaseLogin = () => {
    signInWithPopup(auth, provider)
      .then(async (result) => {
        const credential = GoogleAuthProvider.credentialFromResult(result);
        const token = credential!.accessToken;
        const user = result.user;
        const idToken = await user.getIdToken();
        setAuthToken(idToken);
        console.log(`Auth token: ${idToken}`);
      })
      .catch((error) => {
        console.log(error);
      });
  };

  const startSubscription = () => {
    console.log("Starting subscription")

    const unsubscribe = onSnapshot(collection(db, "cities"), (snapshot) => {
      for (const docChange of snapshot.docChanges()) {
        const city: City = docChange.doc.data() as City

        if (docChange.type === "added") {
          cities.push(city)
        }

        if (docChange.type === "modified") {
          console.log("Modified: ", docChange.doc.data());
        }

        if (docChange.type === "removed") {
          var index = cities.findIndex(c => c.Name === city.Name)
          cities.splice(index, 1)
        }
      }

      setCities(cities)
      forceUpdate() // We need to force update!
    });

    return unsubscribe
  }

  useEffect(() => {
    if (started) {
      return startSubscription()
    }
  }, [started])

  return (
    <div className="App">
      <div>
        <button onClick={firebaseLogin}>Login</button>
      </div>
      <p style={{ wordBreak: "break-all" }}>{authToken}</p>
      <div>
        <button onClick={() => setStarted(true)}>Start Subscription</button>
      </div>
      <div>
        <input type="text" onBlur={(e) => setCityName(e.target.value)} />
        <input type="text" onBlur={(e) => setStateName(e.target.value)} />
        <button onClick={createCity}>Add</button>
      </div>
      <div>
        {
          cities.map((city, index) =>
          (<p key={`city-${index}`}>
            {city.Name}, {city.State}
          </p>)
          )
        }
      </div>
    </div>
  )
}

export default App
