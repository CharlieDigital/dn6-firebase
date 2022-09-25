<script setup lang="ts">
import { onUnmounted, ref } from "vue";
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

interface City {
  State: string
  Name: string
}

const cityName = ref("");
const stateName = ref("");
const authToken = ref("");
const cities = ref<City[]>([])

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
// This routes it to our local emulator; in actual code, add a condition here.
connectAuthEmulator(auth, "http://localhost:10099");

const db = getFirestore(app);
// This routes it to our local emulator; in actual code, add a condition here.
connectFirestoreEmulator(db, "localhost", 8181);

// This handle will hold our unsubscribe function
let unsubscribe: Unsubscribe | undefined;

onUnmounted(() => {
  if (unsubscribe) {
    unsubscribe();
  }
});

/**
 * Function to create a city
 */
const createCity = async () => {
  const city = cityName.value;
  const state = stateName.value;
  const url = `http://localhost:20080/city/add/${state}/${city}`;

  const response = await fetch(url, {
    method: "GET",
    headers: new Headers({
      Authorization: "bearer " + authToken.value,
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
      authToken.value = await user.getIdToken();
      console.log(`Auth token: ${authToken.value}`);
    })
    .catch((error) => {
      console.log(error);
    });
};

/**
 * Start the subscription after login.
 */
const startSubscription = () => {
  unsubscribe = onSnapshot(
    collection(db, "cities"),
    (snapshot) => {
      for (const docChange of snapshot.docChanges()) {
        const city: City = docChange.doc.data() as City

        if (docChange.type === "added") {
          cities.value.push(city)
        }

        if (docChange.type === "modified") {
          console.log("Modified: ", docChange.doc.data());
        }

        if (docChange.type === "removed") {
          var index = cities.value.findIndex(c => c.Name === city.Name)
          cities.value.splice(index, 1)
        }
      }
  });
}
</script>

<template>
  <div>
    <button @click="firebaseLogin">Login</button>
  </div>
  <p>{{ authToken === "" ? "Click Login" : authToken }}</p>
  <div>
    <button @click="startSubscription">Start Subscription</button>
  </div>
  <div>
    <input type="text" v-model="cityName" />
    <input type="text" v-model="stateName" />
    <button @click="createCity">Add</button>
  </div>
  <div>
      <p
        v-for="(city, index) in cities"
        :key="index">
        {{ city.Name + ', ' + city.State }}
      </p>
  </div>
</template>

<style>
p {
  word-break: break-all;
}
</style>
