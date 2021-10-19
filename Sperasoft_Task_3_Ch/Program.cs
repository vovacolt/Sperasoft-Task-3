using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sperasoft_Task_3_Ch
{
    //Узел стека
    public class Node<T>
    {
        public int thr_index;
        public T value;

        public Node<T> next;

        public Node()
        {
            this.thr_index = default(int);
            this.value = default(T);
            this.next = null;
        }

        public Node(int thr_index, T value)
        {
            this.thr_index = thr_index;
            this.value = value;
            this.next = null;
        }
    }

    //Lock-Free стек
    public class LookFreeStaticStack<T>
    {
        private Node<T> top;
        private int capacity;
        private int length;

        public LookFreeStaticStack(int capacity)
        {
            this.capacity = capacity;
            this.top = new Node<T>();
            this.length = 0;
        }

        //Добавить элемент в стэк
        public bool Push(int thr_index, T value)
        {
            Node<T> node = new Node<T>(thr_index, value);
            
            if(length >= capacity)
            {
                Console.WriteLine("Stack overflow!");
                return false;
            }
            else
            {
                do
                {
                    node.next = top.next;
                }
                while (!CompareAndSwap(ref top.next, node, node.next));

                length++;
                return true;
            }
        }

        //Взять элемент из стэка
        public Node<T> Pop()
        {
            Node<T> node;

            do
            {
                node = top.next;

                if (node == null)
                {
                    Console.WriteLine("Stack underflow!");
                    return default(Node<T>);
                }
            }
            while (!CompareAndSwap(ref top.next, node.next, node));

            length--;
            return node;
        }

        //Посмотреть элемент из стэка с максимальным приоритетом, не снимая его со стука
        public Node<T> Show()
        {
            return top.next;
        }

        //Проверяет, пустой ли стэк
        public bool IsEmpty()
        {
            return top.next == null;
        }

        //Очищает стэк
        public void Clear()
        {
            while (!IsEmpty())
            {
                Pop();
            }
        }

        //Текущая заполненность
        public int Length()
        {
            return length;
        }

        //Распечатать содержимое стека в консоль
        public void Print()
        {
            Node<T> node;
            node = top.next;

            Console.WriteLine("Stack from top:");

            while (node != null)
            {
                Console.WriteLine("Thread: " + node.thr_index + " | " + "Value: " + node.value);
                node = node.next;
            }
        }

        //Атомарная операция сравнения с обменом
        private static bool CompareAndSwap(ref Node<T> location, Node<T> new_value, Node<T> comparand)
        {
            var old_location = Interlocked.CompareExchange<Node<T>>(ref location, new_value, comparand);
            return comparand == old_location;
        }
    }

    class Program
    {
        public static void PushAny(ref LookFreeStaticStack<int> stack, int thr_index, int[] values)
        {
            for(int i = 0; i < values.Length; i++)
			{
                stack.Push(thr_index, values[i]);
            }
        }

        public static void PopAny(ref LookFreeStaticStack<int> stack, int thr_index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var elem = stack.Pop();

                if(elem != null)
                {
                    Console.WriteLine("Op. Pop: " + "Thread: " + thr_index + " | " + "Value: "  + elem.value);
                }
                else
                {
                    count++; //чтобы снимать только не null обьекты со стека.
                }
            }
        }

        static async Task Main(string[] args)
        {
            {
                Console.WriteLine("Test: Is Empty");
                Console.WriteLine("_____________________________");

                var stack = new LookFreeStaticStack<int>(1);

                Console.WriteLine("Stack is empty?: " + stack.IsEmpty());

                PushAny(ref stack, 1, new int[] {11});
                Console.WriteLine();

                Console.WriteLine("Push value: " + stack.Show().value);
                Console.WriteLine();

                Console.WriteLine("Stack is empty?: " + stack.IsEmpty());
                Console.WriteLine();

                stack.Print();

                Console.WriteLine("_____________________________");

                //	До добавления элемента стек пуст.
                //	После добавления элемента IsEmpty
                //	говорит, что стек не пуст.

                //  Test: Is Empty
                //  _____________________________
                //  Stack is empty?: True
                //
                //  Push value: 11
                //
                //  Stack is empty?: False
                //
                //  Stack from top:
                //  Thread: 1 | Value: 11
                //  _____________________________
            }

            Thread.Sleep(250);

            {
                Console.WriteLine("Test: Push");
                Console.WriteLine("_____________________________");

                var stack = new LookFreeStaticStack<int>(20);

                new Thread(() => PushAny(ref stack, 1, new int[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 })).Start();
                new Thread(() => PushAny(ref stack, 2, new int[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 })).Start();

                Thread.Sleep(250);

                stack.Print();

                Console.WriteLine("_____________________________");

                //Стек сохраняет принцип LIFO
                //относительно потоков в которых выполняется
                //операция Push.

                //Это отчетливо видно, потому что первый поток
                //пишет только нечетные, а второй только четные.

                //При этом проблема порядка
                //одновременного обращения к памяти не возникает.

                //Так же проблема ABA не возникает
                //за счет использования атомарного CAS.

                //  Test: Push
                //  _____________________________
                //  Stack from top:
                //      Thread: 2 | Value: 20
                //      Thread: 2 | Value: 18
                //      Thread: 2 | Value: 16
                //  Thread: 1 | Value: 19
                //      Thread: 2 | Value: 14
                //      Thread: 2 | Value: 12
                //  Thread: 1 | Value: 17
                //      Thread: 2 | Value: 10
                //      Thread: 2 | Value: 8
                //  Thread: 1 | Value: 15
                //      Thread: 2 | Value: 6
                //  Thread: 1 | Value: 13
                //  Thread: 1 | Value: 11
                //  Thread: 1 | Value: 9
                //  Thread: 1 | Value: 7
                //  Thread: 1 | Value: 5
                //  Thread: 1 | Value: 3
                //      Thread: 2 | Value: 4
                //  Thread: 1 | Value: 1
                //      Thread: 2 | Value: 2
                //_____________________________
            }

            Thread.Sleep(250);

            {
                Console.WriteLine("Test: Pop");
                Console.WriteLine("_____________________________");

                var stack = new LookFreeStaticStack<int>(10);

                PushAny(ref stack, 1, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

                Thread.Sleep(250);

                stack.Print();
                Console.WriteLine();

                Thread.Sleep(250);

                new Thread(() => PopAny(ref stack, 1, 3)).Start();
                new Thread(() => PopAny(ref stack, 2, 3)).Start();

                Thread.Sleep(250);

                Console.WriteLine();
                stack.Print();

                Console.WriteLine("_____________________________");

                //Ошибки одновременного доступа
                //к общей памяти не возникает,
                //благодаря использованию CAS.

                //Таким образом между потоками
                //нет гонки за данными, мы не
                //пытаемся снять со стека 
                //один и тот же элемент со стека.
                
                //Это свойство гарантирует сохранение
                //структурности стека, после 
                //вызова нескольник операций Pop,
                //в разных потоках.

                //Принцип LIFO сохранется в рамках
                //отдельно взятого потока.

                //  Test: Pop
                //  _____________________________
                //  Stack from top:
                //  Thread: 1 | Value: 10
                //  Thread: 1 | Value: 9
                //  Thread: 1 | Value: 8
                //  Thread: 1 | Value: 7
                //  Thread: 1 | Value: 6
                //  Thread: 1 | Value: 5
                //  Thread: 1 | Value: 4
                //  Thread: 1 | Value: 3
                //  Thread: 1 | Value: 2
                //  Thread: 1 | Value: 1
                //
                //  Op.Pop: Thread: 2 | Value: 10
                //  Op.Pop: Thread: 2 | Value: 8
                //  Op.Pop: Thread: 2 | Value: 7
                //  Op.Pop: Thread: 1 | Value: 9
                //  Op.Pop: Thread: 1 | Value: 6
                //  Op.Pop: Thread: 1 | Value: 5
                //
                //  Stack from top:
                //  Thread: 1 | Value: 4
                //  Thread: 1 | Value: 3
                //  Thread: 1 | Value: 2
                //  Thread: 1 | Value: 1
                //  _____________________________
            }

            Thread.Sleep(250);

            {
                Console.WriteLine("Test: Push and Pop");
                Console.WriteLine("_____________________________");

                var stack = new LookFreeStaticStack<int>(10);

                new Thread(() => PushAny(ref stack, 1, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })).Start();

                new Thread(() => PopAny(ref stack, 2, 5)).Start();

                Thread.Sleep(250);

                Console.WriteLine();
                stack.Print();

                Console.WriteLine("_____________________________");

            //Ошибка SegFault не возникнет, так как
            //мы находимся в среде со сборщиком мусора.
            
            //Легко заметить, что снимаем со стека
            //верхние пять элементов.

            //И если первый поток успел записать
            //достаточное количество элементов в стек,
            //то второк поток отработает правильно.

            //  Test: Push and Pop
            //  _____________________________
            //  Op.Pop: Thread: 2 | Value: 10
            //  Op.Pop: Thread: 2 | Value: 9
            //  Op.Pop: Thread: 2 | Value: 8
            //  Op.Pop: Thread: 2 | Value: 7
            //  Op.Pop: Thread: 2 | Value: 6
            //
            //  Stack from top:
            //  Thread: 1 | Value: 5
            //  Thread: 1 | Value: 4
            //  Thread: 1 | Value: 3
            //  Thread: 1 | Value: 2
            //  Thread: 1 | Value: 1
            //  _____________________________

            //Но иногда второй поток
            //обгоняет первый.

            //Тогда несколько операций Pop,
            //могут сработать в пустую.

            //При этом стек сохраняет свою структурно
            //и дальнейшие вызовы Pop работают правильно.

            //Данная ошибка находится на стороне функции,
            //которая снимает элементы со стека.

            //Достаточно ее немного доработать, чтобы
            //она снимала со стека определенное количество
            //не null обьектов.

            //Что я потом и сделал.

            //  Test: Push and Pop
            //  _____________________________
            //  Stack underflow!
            //  Op.Pop: Thread: 2 | Value: 10
            //  Op.Pop: Thread: 2 | Value: 9
            //  Op.Pop: Thread: 2 | Value: 8
            //  Op.Pop: Thread: 2 | Value: 7
            //
            //  Stack from top:
            //  Thread: 1 | Value: 6
            //  Thread: 1 | Value: 5
            //  Thread: 1 | Value: 4
            //  Thread: 1 | Value: 3
            //  Thread: 1 | Value: 2
            //  Thread: 1 | Value: 1
            //  _____________________________
            }

            Thread.Sleep(250);

            {
                Console.WriteLine("Test: Show");
                Console.WriteLine("_____________________________");

                var stack = new LookFreeStaticStack<int>(5);

                PushAny(ref stack, 1, new int[] { 1, 2, 3, 4, 5});

                stack.Print();

                Console.WriteLine();
                Console.WriteLine("Show top: " + stack.Show().value);
                Console.WriteLine();

                stack.Print();

                Console.WriteLine("_____________________________");

                //	Операция Show только показывает
                //	значение вершины, но
                //	не снимает ее со стека.

                //  Test: Show
                //  _____________________________
                //  Stack from top:
                //  Thread: 1 | Value: 5
                //  Thread: 1 | Value: 4
                //  Thread: 1 | Value: 3
                //  Thread: 1 | Value: 2
                //  Thread: 1 | Value: 1
                //
                //  Show top: 5
                //
                //  Stack from top:
                //  Thread: 1 | Value: 5
                //  Thread: 1 | Value: 4
                //  Thread: 1 | Value: 3
                //  Thread: 1 | Value: 2
                //  Thread: 1 | Value: 1
                //  _____________________________
            }

            Thread.Sleep(250);

            {
                Console.WriteLine("Test: Clear");
                Console.WriteLine("_____________________________");

                var stack = new LookFreeStaticStack<int>(5);

                PushAny(ref stack, 1, new int[] { 1, 2, 3, 4, 5 });

                stack.Print();
                stack.Clear();

                Console.WriteLine();
                Console.WriteLine("Clear stack...");
                Console.WriteLine();

                Console.WriteLine("Stack is empty?: " + stack.IsEmpty());

                Console.WriteLine("_____________________________");

                //	После очистки, стек пуст.

                //  Test: Clear
                //  _____________________________
                //  Stack from top:
                //  Thread: 1 | Value: 5
                //  Thread: 1 | Value: 4
                //  Thread: 1 | Value: 3
                //  Thread: 1 | Value: 2
                //  Thread: 1 | Value: 1
                //
                //  Clear stack...
                //
                //  Stack is empty?: True
                //  _____________________________
            }

            await Task.WhenAll();
        }
    }
}
