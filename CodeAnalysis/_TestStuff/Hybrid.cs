namespace CodeAnalysis._TestStuff
{
    class Hybrid
    {
        public int PublicVariableWithMutators { get; set; } //PropertyDeclaration
        public int PublicVariable;                    //FieldDeclaration

        private int PrivateVariableWithMutators { get; set; }
        private int PrivateVariable;

        private int Blbu { get; }

        public void PublicFunction() { }
        private void PrivateFunction() { }
            
    }
}
